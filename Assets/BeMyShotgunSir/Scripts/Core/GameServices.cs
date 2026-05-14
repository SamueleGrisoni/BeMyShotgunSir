using UnityEngine;
using FishNet.Managing;
using BeMyShotgunSir.Scripts.Core.Audio;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;

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
            Lobby.LobbyManager.OnLobbyManagerInitialized += OnLobbyManagerReady;
            Lobby.LobbyManager.OnLobbyManagerDespawned += OnLobbyManagerDespawned;
            Race.RaceManager.OnRaceManagerInitialized += OnRaceManagerReady;
            Race.RaceManager.OnRaceManagerDespawned += OnRaceManagerDespawned;

            TryGetComponent(out _sceneCoordinator);

            AudioManager.Initialize();
            ConnectionManager.Initialize();
        }

        private void OnLobbyManagerReady(ILobbyManager lobbyManager)
        {
            if (LobbyManager != null && LobbyManager != lobbyManager)
            {
                Log.ELazy(() => $"GameServices: Duplicate LobbyManager detected.", this);
                return;
            }

            LobbyManager = lobbyManager;
        }
        private void OnLobbyManagerDespawned() => LobbyManager = null;
        private void OnRaceManagerReady(IRaceManager raceManager)
        {
            if (RaceManager != null && RaceManager != raceManager)
            {
                Log.ELazy(() => $"GameServices: Duplicate RaceManager detected.", this);
                return;
            }
            RaceManager = raceManager;
        }
        private void OnRaceManagerDespawned() => RaceManager = null;

        private void OnDisable()
        {
            Lobby.LobbyManager.OnLobbyManagerInitialized -= OnLobbyManagerReady;
            Lobby.LobbyManager.OnLobbyManagerDespawned -= OnLobbyManagerDespawned;
            Race.RaceManager.OnRaceManagerInitialized -= OnRaceManagerReady;
            Race.RaceManager.OnRaceManagerDespawned -= OnRaceManagerDespawned;
        }

        #region Local

        [SerializeField] private SceneCoordinator _sceneCoordinator;
        public UIFlowState UIFlowState { get; private set; }
        public SceneCoordinator SceneCoordinator => _sceneCoordinator;
        [field: SerializeField] public ConnectionManager ConnectionManager { get; private set; }
        [field: SerializeField] public SOChannels Channels { get; private set; }
        [field: SerializeField] public AudioManager AudioManager { get; private set; }

        #endregion

        #region Network

        [field: SerializeField] public NetworkManager NetworkManager { get; private set; }
        public ILobbyManager LobbyManager { get; private set; }
        public IRaceManager RaceManager { get; private set; }

        #endregion
    }
}
