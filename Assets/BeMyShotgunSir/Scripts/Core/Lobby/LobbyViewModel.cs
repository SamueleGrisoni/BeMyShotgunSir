using System;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbyDataView : IDataView
    {
        LobbyInfo LobbyInfo { get; }
        int PlayerCount { get; }
        IReadOnlyDictionary<int, LobbyPlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, LobbyTeamInfo> TeamInfos { get; }
        event Action OnLobbyInfoChanged;
        event Action OnLobbyIPChanged;
        event Action OnPlayerCountChanged;
        event Action OnLobbyPlayerStatesChanged;
        event Action OnLobbyTeamInfosChanged;
    }

    public class LobbyViewModel : ILobbyDataView
    {
        private bool _log = false;
        private ILobbyNetStateSubscribe _netState;

        public LobbyInfo LobbyInfo => _netState.LobbyInfo;
        public int PlayerCount => _netState.PlayerCount;
        public IReadOnlyDictionary<int, LobbyPlayerState> PlayerStates => _netState.PlayerStates;
        public IReadOnlyDictionary<int, LobbyTeamInfo> TeamInfos => _netState.TeamInfos;

        public event Action OnLobbyInfoChanged;
        public event Action OnLobbyIPChanged;
        public event Action OnPlayerCountChanged;
        public event Action OnLobbyPlayerStatesChanged;
        public event Action OnLobbyTeamInfosChanged;

        public LobbyViewModel(ILobbyNetStateSubscribe netState)
        {
            _netState = netState;
            _netState.LobbyInfo_Sub.OnChange += OnLobbyInfoChanged_Propagate;
            _netState.PlayerCount_Sub.OnChange += OnPlayerCountChanged_Propagate;
            _netState.PlayerStates_Sub.OnChange += OnLobbyPlayerStatesChanged_Propagate;
            _netState.TeamInfos_Sub.OnChange += OnLobbyTeamInfosChanged_Propagate;
        }

        private void OnLobbyInfoChanged_Propagate(LobbyInfo _, LobbyInfo __, bool ___) => OnLobbyInfoChanged?.Invoke();
        private void OnPlayerCountChanged_Propagate(int _, int __, bool ___) => OnPlayerCountChanged?.Invoke();
        private void OnLobbyPlayerStatesChanged_Propagate(SyncDictionaryOperation _, int __, LobbyPlayerState ___, bool ____) => OnLobbyPlayerStatesChanged?.Invoke();
        private void OnLobbyTeamInfosChanged_Propagate(SyncDictionaryOperation _, int __, LobbyTeamInfo ___, bool ____) => OnLobbyTeamInfosChanged?.Invoke();
    }
}
