#pragma warning disable CS0414 // Variabile assegnata ma mai usata
#pragma warning disable CS0169 // Variabile mai usata
#pragma warning disable CS0067

using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceDataView : IDataView
    {
        ILobbyDataView LobbyDataView { get; }
        int Seed { get; }
        IReadOnlyDictionary<int, RacePlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, RaceTeamData> TeamData { get; }
        IReadOnlyList<int> Leaderboard { get; }
        event Action OnSeedChanged;
        event Action OnRacePlayerStatesChanged;
        event Action OnRaceTeamDataChanged;
        event Action OnLeaderboardChanged;
    }

    public class RaceViewModel : IRaceDataView
    {
        private bool _log = false;
        private LobbyViewModel _lobbyViewModel;
        private IRaceNetStateSubscribe _netState;

        public event Action OnSeedChanged;
        public event Action OnRacePlayerStatesChanged;
        public event Action OnRaceTeamDataChanged;
        public event Action OnLeaderboardChanged;

        public int Seed => _netState.Seed;
        public IReadOnlyDictionary<int, RacePlayerState> PlayerStates => _netState.PlayerStates;
        public IReadOnlyDictionary<int, RaceTeamData> TeamData => _netState.TeamData;
        public IReadOnlyList<int> Leaderboard => _netState.Leaderboard;
        public ILobbyDataView LobbyDataView => _lobbyViewModel;

        public RaceViewModel(LobbyViewModel lobbyViewModel, IRaceNetStateSubscribe netState)
        {
            _lobbyViewModel = lobbyViewModel;
            _netState = netState;
            _netState.Seed_Sub.OnChange += OnSeedChanged_Propagate;
            _netState.PlayerStates_Sub.OnChange += OnPlayerStatesChanged_Propagate;
            _netState.TeamData_Sub.OnChange += OnRaceTeamDataChanged_Propagate;
            _netState.Leaderboard_Sub.OnChange += OnLeaderboardChanged_Propagate;
        }

        public void AskForRefresh()
        {
            OnSeedChanged?.Invoke();
            OnRacePlayerStatesChanged?.Invoke();
            OnRaceTeamDataChanged?.Invoke();
            OnLeaderboardChanged?.Invoke();
        }

        private void OnSeedChanged_Propagate(int _, int __, bool ___) => OnSeedChanged?.Invoke();
        private void OnPlayerStatesChanged_Propagate(SyncDictionaryOperation _, int __, RacePlayerState ___, bool ____) => OnRacePlayerStatesChanged?.Invoke();
        private void OnRaceTeamDataChanged_Propagate(SyncDictionaryOperation _, int __, RaceTeamData ___, bool ____) => OnRaceTeamDataChanged?.Invoke();
        private void OnLeaderboardChanged_Propagate(SyncListOperation _, int __, int ___, int ____, bool _____) => OnLeaderboardChanged?.Invoke();
    }
}
