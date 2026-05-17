#pragma warning disable CS0414 // Variabile assegnata ma mai usata
#pragma warning disable CS0169 // Variabile mai usata
#pragma warning disable CS0067

using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceDataView : IDataView
    {
        IRaceNetStateRead NetState { get; }
        int ClientId { get; }
        ILobbyDataView LobbyDataView { get; }
        int? Seed { get; }
        IReadOnlyDictionary<int, RacePlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, RaceTeamData> TeamData { get; }
        IReadOnlyDictionary<int, InventoryData> TeamInventories { get; }
        IReadOnlyList<int> Leaderboard { get; }
        event Action OnSeedChanged;
        event Action OnRacePlayerStatesChanged;
        event Action OnInventoryChanged;
        event Action OnRaceTeamDataChanged;
        event Action OnLeaderboardChanged;
        event Action OnTeamTrackProgressChanged;
        event Action OnShowCountdown;
        event Action OnShowFinishScreenChanged;

    }

    public class RaceViewModel : IRaceDataView
    {
        private bool _log = false;
        private LobbyViewModel _lobbyViewModel;
        private IRaceNetStateSubscribe _netStateSub;
        private RaceNetStateStore _netState;
        public IRaceNetStateRead NetState => _netState;
        public int ClientId { get; private set; }

        public event Action OnSeedChanged;
        public event Action OnRacePlayerStatesChanged;
        public event Action OnRaceTeamDataChanged;
        public event Action OnLeaderboardChanged;
        public event Action OnInventoryChanged;
        public event Action OnTeamTrackProgressChanged;
        public event Action OnShowCountdown;
        public event Action OnShowFinishScreenChanged;

        public int? Seed => _netState.Seed;
        public ILobbyDataView LobbyDataView => _lobbyViewModel;
        public IReadOnlyDictionary<int, RacePlayerState> PlayerStates => _netState.PlayerStates;
        public IReadOnlyDictionary<int, RaceTeamData> TeamData => _netState.TeamData;
        public IReadOnlyDictionary<int, InventoryData> TeamInventories => _netState.PlayerInventories;
        public IReadOnlyList<int> Leaderboard => _netState.Leaderboard;
        public IReadOnlyDictionary<int, TeamTrackProgress> TeamTrackProgress => _netState.TeamTrackProgress;
        public bool ShowFinishScreen { get; private set; } = false;

        public RaceViewModel(LobbyViewModel lobbyViewModel, RaceNetStateStore netState, int clientId)
        {
            ClientId = clientId;
            _lobbyViewModel = lobbyViewModel;
            _netState = netState;
            _netStateSub.Seed_Sub.OnChange += OnSeedChanged_Propagate;
            _netStateSub.PlayerStates_Sub.OnChange += OnPlayerStatesChanged_Propagate;
            _netStateSub.TeamData_Sub.OnChange += OnRaceTeamDataChanged_Propagate;
            _netStateSub.PlayerInventories_Sub.OnChange += OnInventoryChanged_Propagate;
            _netStateSub.Leaderboard_Sub.OnChange += OnLeaderboardChanged_Propagate;
            _netStateSub.TeamTrackProgress_Sub.OnChange += OnTeamTrackProgressChanged_Propagate;
        }


        public void AskForRefresh()
        {
            OnSeedChanged?.Invoke();
            OnRacePlayerStatesChanged?.Invoke();
            OnRaceTeamDataChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            OnLeaderboardChanged?.Invoke();
            OnTeamTrackProgressChanged?.Invoke();
        }

        private void OnSeedChanged_Propagate(int? _, int? __, bool ___) => OnSeedChanged?.Invoke();
        private void OnPlayerStatesChanged_Propagate(SyncDictionaryOperation _, int __, RacePlayerState ___, bool ____) => OnRacePlayerStatesChanged?.Invoke();
        private void OnInventoryChanged_Propagate(SyncDictionaryOperation op, int key, InventoryData value, bool asServer) => OnInventoryChanged?.Invoke();
        private void OnRaceTeamDataChanged_Propagate(SyncDictionaryOperation _, int __, RaceTeamData ___, bool ____) => OnRaceTeamDataChanged?.Invoke();
        private void OnLeaderboardChanged_Propagate(SyncListOperation _, int __, int ___, int ____, bool _____) => OnLeaderboardChanged?.Invoke();
        private void OnTeamTrackProgressChanged_Propagate(SyncDictionaryOperation _, int __, TeamTrackProgress ___, bool ____) => OnTeamTrackProgressChanged?.Invoke();

        public bool TryGetTeamIdFromClientId(int clientId, out int? teamId)
        {
            teamId = null;
            if (_netState.PlayerStates.TryGetValue(clientId, out RacePlayerState playerState))
            {
                teamId = playerState.TeamId;
                return true;
            }
            Log.WLazy(() => $"Trying to get team id for client {clientId} but no player state found.", this);
            return false;
        }

        public void SetShowFinishScreen(bool v)
        {
            ShowFinishScreen = v;
            OnShowFinishScreenChanged?.Invoke();
        }

        public void SetShowCountdown() => OnShowCountdown?.Invoke();
    }
}
