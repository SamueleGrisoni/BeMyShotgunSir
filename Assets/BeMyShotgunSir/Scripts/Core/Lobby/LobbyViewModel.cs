using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbyNetData : INetData
    {
        LobbyInfo LobbyInfo { get; }
        int PlayerCount { get; }
        Dictionary<int, PlayerLobbyState> PlayerStates { get; }
    }

    public interface ILobbyData : IData { }

    public interface ILobbyDataView : IDataView, ILobbyData, ILobbyNetData
    {
        event Action OnLobbyInfoChanged;
        event Action OnLobbyIPChanged;
        event Action OnPlayerCountChanged;
        event Action OnPlayerStatesChanged;
    }

    public class LobbyViewModel : ViewModel<ILobbyData, ILobbyNetData>, ILobbyDataView
    {
        public override void InitData(ILobbyData data = null)
        {
            LobbyIP = "127.0.0.1";
            PlayerCount = 0;
        }

        public override void InitNetData(ILobbyNetData data)
        {
            if (data is not ILobbyNetData lobbyData)
            {
                Log.ELazy(() => "InitNetData called with invalid data type. Expected ILobbyNetData.", this);
                return;
            }

            //no setters to allow a real refresh even for unchanged values
            LobbyInfo = lobbyData.LobbyInfo;
            OnLobbyInfoChanged?.Invoke();
            LobbyIP = lobbyData.LobbyInfo.LobbyIP;
            OnLobbyIPChanged?.Invoke();
            PlayerCount = lobbyData.PlayerCount;
            OnPlayerCountChanged?.Invoke();
            PlayerStates = lobbyData.PlayerStates;
            OnPlayerStatesChanged?.Invoke();
        }

        public LobbyInfo LobbyInfo { get; private set; }
        public event Action OnLobbyInfoChanged;
        public void SetLobbyInfo(LobbyInfo newInfo)
        {
            LobbyInfo = newInfo;
            OnLobbyInfoChanged?.Invoke();
        }

        public string LobbyIP { get; private set; }
        public event Action OnLobbyIPChanged;
        public void SetLobbyIP(string newIP)
        {
            if (LobbyIP != newIP)
            {
                LobbyIP = newIP;
                OnLobbyIPChanged?.Invoke();
            }
        }

        public int PlayerCount { get; private set; }
        public event Action OnPlayerCountChanged;
        public void SetPlayerCount(int newCount)
        {
            if (PlayerCount != newCount)
            {
                PlayerCount = newCount;
                OnPlayerCountChanged?.Invoke();
            }
        }

        public Dictionary<int, PlayerLobbyState> PlayerStates { get; private set; } = new Dictionary<int, PlayerLobbyState>();
        public event Action OnPlayerStatesChanged;
        public void SetPlayerStates(SyncDictionaryOperation op, int key, PlayerLobbyState value)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                    PlayerStates[key] = value;
                    break;
                case SyncDictionaryOperation.Remove:
                    PlayerStates.Remove(key);
                    break;
                case SyncDictionaryOperation.Clear:
                    PlayerStates.Clear();
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
            OnPlayerStatesChanged?.Invoke();
        }
    }
}
