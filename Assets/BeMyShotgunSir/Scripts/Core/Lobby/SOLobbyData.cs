using System;
using BeMyShotgunSir.Core;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    [CreateAssetMenu(fileName = "LobbyData", menuName = "Be My Shotgun, Sir!/RuntimeData/LobbyData")]
    public class SOLobbyData : SOData, ILobbyDataView
    {
        public override void InitData()
        {
            LobbyIP = "127.0.0.1";
            PlayerNames = new string[0];
            PlayerCount = 0;
        }

        public override void InitNetData(INetData data)
        {
            if (data is ILobbyNetData lobbyData)
            {
                SetLobbyIP(lobbyData.LobbyIP);
                SetPlayerCount(lobbyData.PlayerCount);
                SetPlayerNames(lobbyData.PlayerNames);
            }
            //NOTE: maybe one single event here? Then the UI must be aware of this
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
        public string[] PlayerNames { get; private set; }
        public event Action OnPlayerNamesChanged;
        public void SetPlayerNames(string[] newPlayerNames)
        {
            PlayerNames = newPlayerNames;
            OnPlayerNamesChanged?.Invoke();
        }

    }
}
