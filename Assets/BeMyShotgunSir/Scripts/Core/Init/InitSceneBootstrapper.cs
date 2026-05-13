using System;
using UnityEngine.SceneManagement;

namespace BeMyShotgunSir.Scripts.Core.Init
{
    public class InitSceneBootstrapper : SceneBootstrapper
    {
        public static event Action OnInitSceneInitialized;
        private void Start() => Bootstrap();
        protected override void Bootstrap()
        {
            if (SceneManager.GetSceneByName(SceneName.Lobby.ToString()).isLoaded)
                SceneManager.UnloadSceneAsync(SceneName.Lobby.ToString());
            OnInitSceneInitialized?.Invoke();

            // if (!SceneManager.GetSceneByName(SceneName.UI.ToString()).isLoaded)
            //     SceneManager.LoadScene(SceneName.UI.ToString(), LoadSceneMode.Additive);
        }
    }
}
