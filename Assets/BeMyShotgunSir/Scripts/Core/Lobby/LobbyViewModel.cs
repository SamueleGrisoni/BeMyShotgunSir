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
        private bool _log = true;
        public override void InitData(ILobbyData data = null)
        {
            Log.DLazy(() => "Initializing LobbyViewModel.", this, _log);
            LobbyIP = "127.0.0.1";
            PlayerCount = 0;
        }

        public override void InitNetData(ILobbyNetData data)
        {
            Log.DLazy(() => "Initializing LobbyViewModel net data.", this, _log);
            //no setters to allow a real refresh even for unchanged values
            LobbyInfo = data.LobbyInfo;
            OnLobbyInfoChanged?.Invoke();
            LobbyIP = data.LobbyInfo.LobbyIP;
            OnLobbyIPChanged?.Invoke();
            PlayerCount = data.PlayerCount;
            OnPlayerCountChanged?.Invoke();
            PlayerStates = data.PlayerStates;
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
