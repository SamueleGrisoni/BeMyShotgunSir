using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbySceneInitializer : SceneInitializer
    {
        [SerializeField] private InterfaceSerializer<LobbyBindTarget, ILobbyBindTarget>[] _bindTargets;
        protected override void Initializer()
        {
            var coercedTargets = new ILobbyBindTarget[_bindTargets.Length];
            for (int i = 0; i < _bindTargets.Length; i++)
            {
                if (_bindTargets[i].Interface == null)
                {
                    Log.ELazy(() => $"LobbySceneInitializer: Bind target at index {i} does not implement ILobbyBindTarget. Skipping.", this);
                    continue;
                }
                coercedTargets[i] = _bindTargets[i].Interface;
            }
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneName.Init.ToString());

            LobbyManager.OnLobbySpawned += lobbyManager => lobbyManager.BindLobby(coercedTargets);
        }
    }
}
