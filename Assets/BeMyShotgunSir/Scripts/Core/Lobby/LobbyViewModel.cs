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
        Dictionary<int, LobbyPlayerState> PlayerStates { get; }
        Dictionary<long, LobbyTeamInfo> TeamInfos { get; }
    }

    public interface ILobbyData : IData { }

    public interface ILobbyDataView : IDataView, ILobbyData, ILobbyNetData
    {
        event Action OnLobbyInfoChanged;
        event Action OnLobbyIPChanged;
        event Action OnPlayerCountChanged;
        event Action OnPlayerStatesChanged;
        event Action OnTeamInfosChanged;
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
            TeamInfos = data.TeamInfos;
            OnTeamInfosChanged?.Invoke();
        }

        public LobbyInfo LobbyInfo { get; private set; }
        public event Action OnLobbyInfoChanged;
        public void SetLobbyInfo(LobbyInfo newInfo)
        {
            LobbyInfo = newInfo;
            Log.DLazy(() => $"LobbyInfo updated. {LobbyInfo}", this, _log);
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
            Log.DLazy(() => $"LobbyIP updated to {LobbyIP}.", this, _log);
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
            Log.DLazy(() => $"PlayerCount updated to {PlayerCount}.", this, _log);
        }

        public Dictionary<int, LobbyPlayerState> PlayerStates { get; private set; } = new Dictionary<int, LobbyPlayerState>();
        public event Action OnPlayerStatesChanged;
        public void SetPlayerStates(SyncDictionaryOperation op, int key, LobbyPlayerState value)
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
            Log.DLazy(() => $"PlayerStates updated. Operation: {op}, Key: {key}, Value: {value}", this, _log);
            OnPlayerStatesChanged?.Invoke();
        }

        public Dictionary<long, LobbyTeamInfo> TeamInfos { get; private set; } = new Dictionary<long, LobbyTeamInfo>();
        public event Action OnTeamInfosChanged;
        public void SetTeamInfos(SyncDictionaryOperation op, long key, LobbyTeamInfo value)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                    TeamInfos[key] = value;
                    break;
                case SyncDictionaryOperation.Remove:
                    TeamInfos.Remove(key);
                    break;
                case SyncDictionaryOperation.Clear:
                    TeamInfos.Clear();
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
            Log.DLazy(() => $"TeamInfos updated. Operation: {op}, Key: {key}, Value: {value}", this, _log);
            OnTeamInfosChanged?.Invoke();
        }
    }
}
