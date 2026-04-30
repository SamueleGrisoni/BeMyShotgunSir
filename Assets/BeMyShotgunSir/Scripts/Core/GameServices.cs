using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using FishNet.Managing;
using BeMyShotgunSir.Scripts.Gameplay.Track;
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
            Lobby.LobbyManager.OnLobbyManagerReady += OnLobbyManagerReady;
            Lobby.LobbyManager.OnLobbyManagerDespawned += OnLobbyManagerDespawned;
            Race.RaceManager.OnRaceManagerReady += OnRaceManagerReady;
            Race.RaceManager.OnRaceManagerDespawned += OnRaceManagerDespawned;

            TryGetComponent(out _sceneCoordinator);

            AudioManager.Initialize();
            ConnectionManager.Initialize();
        }

        private void OnLobbyManagerReady(ILobbyManager lobby)
        {
            if (LobbyManager != null && LobbyManager != lobby)
            {
                Log.ELazy(() => $"GameServices: Duplicate LobbyManager detected.", this);
                return;
            }

            LobbyManager = lobby;
        }
        private void OnLobbyManagerDespawned() => LobbyManager = null;
        private void OnRaceManagerReady(IRaceManager manager)
        {
            if (RaceManager != null && RaceManager != manager)
            {
                Log.ELazy(() => $"GameServices: Duplicate RaceManager detected.", this);
                return;
            }
            RaceManager = manager;
        }
        private void OnRaceManagerDespawned() => RaceManager = null;

        private void OnDisable()
        {
            Lobby.LobbyManager.OnLobbyManagerReady -= OnLobbyManagerReady;
            Lobby.LobbyManager.OnLobbyManagerDespawned -= OnLobbyManagerDespawned;
            Race.RaceManager.OnRaceManagerReady -= OnRaceManagerReady;
            Race.RaceManager.OnRaceManagerDespawned -= OnRaceManagerDespawned;
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
        //NOTE now full interfaces, but this should only be used by bootstrapper.
        //So we will probably add an ad hoc bootstrapp instance locator service
        public ILobbyManager LobbyManager { get; private set; }
        public IRaceManager RaceManager { get; private set; }
        #endregion
    }
}
