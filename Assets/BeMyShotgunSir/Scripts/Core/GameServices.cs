using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using FishNet.Managing;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Core.Audio;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;

namespace BeMyShotgunSir.Scripts.Core
{
    [RequireComponent(typeof(SceneCoordinator))]
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

            UIFlowState = new UIFlowState();
        }
        private void Start()
        {
            LobbyManager.OnLobbyManagerSpawned += HandleLobbyManagerSpawned;
            LobbyManager.OnLobbyManagerDespawned += HandleLobbyManagerDespawned;

            TryGetComponent(out _sceneCoordinator);

            AudioManager.Initialize();
            ConnectionManager.Initialize();
        }

        private void HandleLobbyManagerSpawned(LobbyManager lobby)
        {
            if (LobbyManager != null && LobbyManager != lobby)
            {
                Debug.LogError($"GameServices: Duplicate LobbyManager detected. Keeping '{LobbyManager.name}' and ignoring '{lobby.name}'.", this);
                return;
            }

            LobbyManager = lobby;
        }
        private void HandleLobbyManagerDespawned() => LobbyManager = null;

        private void OnDisable()
        {
            LobbyManager.OnLobbyManagerSpawned -= HandleLobbyManagerSpawned;
            LobbyManager.OnLobbyManagerDespawned -= HandleLobbyManagerDespawned;
        }

        #region Local
        [field: SerializeField] public Camera MainCamera { get; private set; }
        [field: SerializeField] public EventSystem EventSystem { get; private set; }
        [field: SerializeField] public AudioListener AudioListener { get; private set; }
        [field: SerializeField] public Volume GlobalVolume { get; private set; }
        [SerializeField] private SceneCoordinator _sceneCoordinator;
        public UIFlowState UIFlowState { get; private set; }
        public SceneCoordinator SceneCoordinator => _sceneCoordinator;
        [field: SerializeField] public ConnectionManager ConnectionManager { get; private set; }
        [field: SerializeField] public SOChannels Channels { get; private set; }
        [field: SerializeField] public AudioManager AudioManager { get; private set; }
        public TrackManager TrackManager { get; private set; }
        #endregion

        #region Network
        [field: SerializeField] public NetworkManager NetworkManager { get; private set; }
        public LobbyManager LobbyManager { get; private set; }
        public RaceManager RaceManager { get; private set; }
        #endregion
    }
}
