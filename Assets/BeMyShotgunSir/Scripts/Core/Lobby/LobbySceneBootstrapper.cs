using System;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbySceneBootstrapper : SceneBootstrapper
    {
        /// <summary>
        /// Avoid subscribing to this event directly. Instead, subscribe to FishNetSceneAdapter.OnSceneInitialized and check for SceneName to determine when a scene is initialized.
        /// </summary>
        public static event Action OnLobbySceneInitialized;

        [SerializeField] private InterfaceSerializer<LobbyBindTarget, ILobbyBindTarget>[] _bindTargets;
        private ILobbyBindTarget[] _coercedTargets;

        private void OnDisable() =>
            LobbyManager.OnLobbyManagerInitialized -= OnLobbyManagerReady;

        private void Bind(ILobbyManager_Bootstrapper manager)
        {
            if (_coercedTargets == null)
            {
                Log.ELazy(() => "Coerced bind targets are null. Cannot bind lobby.", this);
                return;
            }
            manager.BindLobby_Initial(_coercedTargets);
            OnLobbySceneInitialized?.Invoke();
        }

        private void OnLobbyManagerReady(ILobbyManager manager)
        {
            if (manager == null || manager is not ILobbyManager_Bootstrapper manager_Bootstrapper)
            {
                Log.ELazy(() => "LobbyManager reference is not of type ILobbyManager_Bootstrapper. Cannot bind lobby.", this);
                return;
            }

            TryInitialize();
            Bind(manager_Bootstrapper);
        }

        protected override void Initialize()
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
            _suppressAutoInitialize = true;

            var lobbyManager = GameServices.Instance.LobbyManager as ILobbyManager_Bootstrapper;
            if (lobbyManager == null)
                LobbyManager.OnLobbyManagerInitialized += OnLobbyManagerReady;
            else
                Bind(lobbyManager);
        }
    }
}
