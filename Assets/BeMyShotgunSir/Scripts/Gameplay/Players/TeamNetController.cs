using System;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Cinemachine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public interface ITeamNetControllerInitializer
    {
        void Initialize(RaceNetContext context, IRoadManager roadManager);
        int? TeamId { get; }
    }

    public class TeamNetController : NetworkBehaviour, ITeamNetControllerInitializer
    {
        //utility
        private bool _log = true;
        private bool _setUpDone = false;
        public static event Action<ITeamNetControllerInitializer> OnTeamSpawned;

        //CONTEXT
        private RaceNetContext _raceNetContext;
        private IRaceNetController _raceNetController;
        private LobbyNetContext _lobbyNetContext;
        private LobbyNetStateStore _lobbyNetStateStore;
        private RaceNetStateStore _netState;
        private InputPublisher _inputPublisher;
        public IRaceNetStateRead NetState => _netState;

        //Specific
        private IRoadManager _roadManager;
        public int? TeamId => _syncTeamId.Value;
        public event Action OnTeamIdAssigned;
        private int? _driverConnectionId = null;
        private int? _shotgunConnectionId = null;
        private DriverController _driverController;
        private ShotgunController _shotgunController;
        private RaceRole _assignedRole;

        private readonly SyncVar<int?> _syncTeamId = new(null);

        private void OnEnable()
        {
            DriverController.OnDriverSpawned += OnDriverSpawned;
            ShotgunController.OnShotgunSpawned += OnShotgunSpawned;
        }

        [Server]
        public void SetTeamId(int teamId)
        {
            _syncTeamId.Value = teamId;
            OnTeamIdAssigned?.Invoke();
            Log.DLazy(() => $"TeamNetController assigned to team {_syncTeamId.Value}.", this, _log);
            if (TeamId.HasValue)
            {
                _raceNetContext.NetState.TryGetTeamTrackProgress(TeamId.Value, out TeamTrackProgress progress);
                _raceNetContext.NetState.SetTeamTrackProgress(TeamId.Value, new TeamTrackProgress(progress, 0, null, new PortalInfo(0, RoadChunkType.START_LINE)));
            }
            else
                Log.ELazy(() => $"MovementController initialized without TeamId.", this);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnTeamSpawned?.Invoke(this);
        }

        public void Initialize(RaceNetContext context, IRoadManager roadManager)
        {
            _raceNetContext = context;
            _inputPublisher = context.InputPublisher;
            _raceNetController = context.NetController;
            _lobbyNetContext = context.LobbyNetContext;
            _lobbyNetStateStore = _lobbyNetContext.NetState;
            _netState = context.NetState;
            _roadManager = roadManager;
            _assignedRole = NetState.PlayerStates[LocalConnection.ClientId].Role;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _syncTeamId.OnChange += TrySetUpTeam;
            TrySetUpTeam();
        }

        public void OnDriverSpawned(DriverController driverController)
        {
            if (_setUpDone)
                return;
            TrySetUpTeam();
        }
        public void OnShotgunSpawned(ShotgunController shotgunController)
        {
            if (_setUpDone)
                return;
            TrySetUpTeam();
        }

        private void TrySetUpTeam()
        {
            if (_setUpDone)
                return;
            TryResolveSpawnedMembers();
            TrySetUpTeam(null, null, false);
        }

        private void TryResolveSpawnedMembers()
        {
            if (_setUpDone)
                return;
            if (_driverController == null)
                _driverController = GetComponentInChildren<DriverController>(true);

            if (_shotgunController == null)
                _shotgunController = GetComponentInChildren<ShotgunController>(true);
        }

        private void TrySetUpTeam(int? _, int? __, bool ___)
        {
            if (_setUpDone)
                return;
            if (_syncTeamId.Value is not int teamId)
                return;

            name = $"Team {teamId}, LocalConnId: {LocalConnection.ClientId}";

            if (_setUpDone || _driverController == null || _shotgunController == null)
                return;

            if (!_lobbyNetStateStore.TryGetPlayersIDs(_syncTeamId.Value, out _driverConnectionId, out _shotgunConnectionId))
            {
                Log.ELazy(() => $"Failed to auto-initialize TeamNetController for ConnectionId: {LocalConnection.ClientId}. No team or player IDs found with team ID {_syncTeamId.Value}.", this, _log);
                return;
            }

            _driverController.Initialize(_raceNetContext, this);
            _shotgunController.Initialize(_raceNetContext, this);
            _driverController.SetName($"Driver, TeamId: {_syncTeamId.Value}, DriverId: {_driverConnectionId}");
            _shotgunController.SetName($"Shotgun, TeamId: {_syncTeamId.Value}, ShotgunId: {_shotgunConnectionId}");

            // for local player only if member of the team
            if (LocalConnection.ClientId == _driverConnectionId || LocalConnection.ClientId == _shotgunConnectionId)
            {
                _roadManager.SetDriver(_driverController.GetMovementTransform());
                Log.DLazy(() => $"Initializing team {teamId} for local player with ConnectionId: {LocalConnection.ClientId}. Driver ConnectionId: {_driverConnectionId}, Shotgun ConnectionId: {_shotgunConnectionId}. Transform: {_driverController.GetMovementTransform().name}", this, _log);
                CinemachineCamera cam = _driverController.GetComponentInChildren<CinemachineCamera>();
                if (cam == null)
                {
                    Log.WLazy(() => $"No CinemachineCamera found in children of TeamNetController for team {TeamId}.", this);
                    return;
                }
                cam.enabled = true;
            }

            _syncTeamId.OnChange -= TrySetUpTeam;
            _setUpDone = true;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            DriverController.OnDriverSpawned -= OnDriverSpawned;
            ShotgunController.OnShotgunSpawned -= OnShotgunSpawned;
        }
    }
}
