using System;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbySceneBootstrapper : SceneBootstrapper
    {
        public static event Action OnLobbySceneInitialized;

        [SerializeField] private InterfaceSerializer<LobbyBindTarget, ILobbyBindTarget>[] _bindTargets;
        private ILobbyBindTarget[] _coercedTargets;

        private void OnDisable() =>
            LobbyManager.OnLobbyManagerSpawned -= OnLobbyManagerSpawned;

        private void Bind(LobbyManager manager)
        {
            if (_coercedTargets == null)
            {
                Log.ELazy(() => "Coerced bind targets are null. Cannot bind lobby.", this);
                return;
            }
            manager.BindLobby(_coercedTargets);
            OnLobbySceneInitialized?.Invoke();
        }

        private void OnLobbyManagerSpawned(LobbyManager manager)
        {
            TryInitialize();
            Bind(manager);
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

            LobbyManager lobbyManager = GameServices.Instance.LobbyManager;
            if (lobbyManager == null)
                LobbyManager.OnLobbyManagerSpawned += OnLobbyManagerSpawned;
            else
                Bind(lobbyManager);
        }
    }
}
