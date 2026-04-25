using System;
using System.Collections.Generic;
using BeMyShotgunSir.Core;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    [CreateAssetMenu(fileName = "LobbyData", menuName = "Be My Shotgun, Sir!/RuntimeData/LobbyData")]
    public class SOLobbyData : SOData, ILobbyDataView
    {
        public override void InitData()
        {
            LobbyIP = "127.0.0.1";
            PlayerCount = 0;
        }

        public override void InitNetData(INetData data)
        {
            if (data is ILobbyNetData lobbyData)
            {
                //no setters to allow a real refresh even for unchanged values
                LobbyIP = lobbyData.LobbyIP;
                OnLobbyIPChanged?.Invoke();
                PlayerCount = lobbyData.PlayerCount;
                OnPlayerCountChanged?.Invoke();
                PlayerStates = lobbyData.PlayerStates;
                OnPlayerStatesChanged?.Invoke();
            }
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

        public LobbyInfo LobbyInfo { get; private set; }
        public event Action OnLobbyInfoChanged;
        public void SetLobbyInfo(LobbyInfo newInfo)
        {
            LobbyInfo = newInfo;
            OnLobbyInfoChanged?.Invoke();
        }
    }
}
