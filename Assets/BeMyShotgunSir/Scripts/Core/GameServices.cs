using UnityEngine;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Core.Audio;
using BeMyShotgunSir.Scripts.Core.Lobby;
using FishNet.Managing;

namespace BeMyShotgunSir.Scripts.Core
{
    public class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            LobbyManager.OnLobbySpawned += HandleLobbySpawned;
            LobbyManager.OnLobbyDespawned += HandleLobbyDespaired;

            AudioManager.Initialize();
            ConnectionManager.Initialize();
        }

        private void HandleLobbySpawned(LobbyManager lobby)
        {
            if (LobbyManager != null && LobbyManager != lobby)
            {
                Debug.LogError($"GameServices: Duplicate LobbyManager detected. Keeping '{LobbyManager.name}' and ignoring '{lobby.name}'.", this);
                return;
            }

            LobbyManager = lobby;
        }
        private void HandleLobbyDespaired() => LobbyManager = null;

        private void OnDisable()
        {
            LobbyManager.OnLobbySpawned -= HandleLobbySpawned;
            LobbyManager.OnLobbyDespawned -= HandleLobbyDespaired;
        }

        [field: SerializeField] public ConnectionManager ConnectionManager { get; private set; }
        [field: SerializeField] public NetworkManager NetworkManager { get; private set; }
        [field: SerializeField] public SOChannels Channels { get; private set; }
        [field: SerializeField] public LobbyManager LobbyManager { get; private set; }
        [field: SerializeField] public AudioManager AudioManager { get; private set; }
        [field: SerializeField] public TrackManager TrackManager { get; private set; }



    }
}
