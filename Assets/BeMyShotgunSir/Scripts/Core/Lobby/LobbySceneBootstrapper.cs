using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbySceneBootstrapper : SceneBootstrapper
    {
        [SerializeField] private InterfaceSerializer<LobbyBindTarget, ILobbyBindTarget>[] _bindTargets;
        private ILobbyBindTarget[] _coercedTargets;

        private void OnDisable() =>
            LobbyManager.OnLobbyManagerSpawned -= OnLobbyManagerSpawned;

        private void OnLobbyManagerSpawned(LobbyManager manager)
        {
            TryInitialize();
            manager.BindLobby(_coercedTargets);
        }

        protected override void Initialize()
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneName.Init.ToString());

            _coercedTargets = new ILobbyBindTarget[_bindTargets.Length];
            for (int i = 0; i < _bindTargets.Length; i++)
            {
                if (_bindTargets[i].Interface == null)
                {
                    Log.ELazy(() => $"LobbySceneInitializer: Bind target at index {i} does not implement ILobbyBindTarget. Skipping.", this);
                    continue;
                }
                _coercedTargets[i] = _bindTargets[i].Interface;
            }
            _suppressAutoInitialize = true;

            LobbyManager lobbyManager = GameServices.Instance.LobbyManager;
            if (lobbyManager == null)
                LobbyManager.OnLobbyManagerSpawned += OnLobbyManagerSpawned;
            else lobbyManager.BindLobby(_coercedTargets);
        }
    }
}
