using System;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbySceneBootstrapperInitializer
    {
        void Initialize(ILobbyManager_Bootstrapper lobbyManager);
    }

    public class LobbySceneBootstrapper : SceneBootstrapper, ILobbySceneBootstrapperInitializer
    {
        private bool _isInitialized = false;
        public static event Action<ILobbySceneBootstrapperInitializer> OnLobbyBootStrapperAwakened;
        public static event Action OnLobbySceneInitialized;

        [SerializeField] private InterfaceSerializer<LobbyBindTarget, ILobbyBindTarget>[] _bindTargets;
        private ILobbyBindTarget[] _coercedTargets;
        private ILobbyManager_Bootstrapper _lobbyManager;

        private void Awake() =>
             OnLobbyBootStrapperAwakened?.Invoke(this);

        private void OnEnable()
        {
            if (!_isInitialized)
                LobbyManager.OnLobbyManagerInitialized += Initialize;
        }

        public void Initialize(ILobbyManager_Bootstrapper manager)
        {
            if (_isInitialized)
                return;
            _lobbyManager = manager;
            Bootstrap();
            _isInitialized = true;
            LobbyManager.OnLobbyManagerInitialized -= Initialize;
        }

        protected override void Bootstrap()
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneName.Init.ToString());

            _coercedTargets = new ILobbyBindTarget[_bindTargets.Length];
            for (int i = 0; i < _bindTargets.Length; i++)
            {
                if (_bindTargets[i].Interface == null)
                {
                    Log.ELazy(() => $"Bind target at index {i} does not implement ILobbyBindTarget. Skipping.", this);
                    continue;
                }
                _coercedTargets[i] = _bindTargets[i].Interface;
            }
            Bind();
        }

        private void Bind()
        {
            if (_coercedTargets == null)
            {
                Log.ELazy(() => "Coerced bind targets are null. Cannot bind lobby.", this);
                return;
            }
            _lobbyManager.BindLobby_Initial(_coercedTargets);
            OnLobbySceneInitialized?.Invoke();
        }
    }
}
