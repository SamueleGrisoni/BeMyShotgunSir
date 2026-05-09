using System;
using BeMyShotgunSir.Scripts.Core;
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
        void OnDriverSpawned(IDriverController driverController);
        void OnShotgunSpawned(IShotgunController shotgunController);
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
        public int TeamId => _syncTeamId.Value;
        private int _driverConnectionId = (int)Codes.UnInitialized;
        private int _shotgunConnectionId = (int)Codes.UnInitialized;
        private IDriverController _driverController;
        private IShotgunController _shotgunController;
        private RaceRole _assignedRole;
        private event Action OnMemberSetUpComplete;

        private readonly SyncVar<int> _syncTeamId = new((int)Codes.UnInitialized);

        private void OnEnable()
        {
            DriverController.OnDriverSpawned += OnDriverSpawned;
            ShotgunController.OnShotgunSpawned += OnShotgunSpawned;
        }

        [Server]
        public void SetTeamId(int teamId)
        {
            _syncTeamId.Value = teamId;
            Log.DLazy(() => $"TeamNetController assigned to team {_syncTeamId.Value}.", this, _log);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnTeamSpawned?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _syncTeamId.OnChange += TrySetUpTeam;
        }

        private void TrySetUpTeam(int oldTeamId, int newTeamId, bool asServer)
        {
            if (_setUpDone || _syncTeamId.Value == (int)Codes.UnInitialized || _driverController == null || _shotgunController == null)
                return;

            if (!_lobbyNetStateStore.TryGetPlayersIDs(_syncTeamId.Value, out _driverConnectionId, out _shotgunConnectionId))
            {
                Log.ELazy(() => $"Failed to auto-initialize TeamNetController for ConnectionId: {LocalConnection.ClientId}. No team or player IDs found with team ID {_syncTeamId.Value}.", this, _log);
                return;
            }

            _driverController.Initialize(_raceNetContext, this);
            _shotgunController.Initialize(_raceNetContext, this);
            _roadManager.SetDriver(_driverController.GetMovementTransform());

            // Enable camera for local player only if member of the team
            if (LocalConnection.ClientId == _driverConnectionId || LocalConnection.ClientId == _shotgunConnectionId)
            {
                CinemachineCamera cam = GetComponentInChildren<CinemachineCamera>();
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

        public void OnDriverSpawned(IDriverController driverController)
        {
            _driverController = driverController;
            TrySetUpTeam(0, 0, false);
        }

        public void OnShotgunSpawned(IShotgunController shotgunController)
        {
            _shotgunController = shotgunController;
            TrySetUpTeam(0, 0, false);
        }
    }
}
