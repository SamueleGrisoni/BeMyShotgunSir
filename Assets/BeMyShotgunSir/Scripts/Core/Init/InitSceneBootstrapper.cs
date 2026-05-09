using UnityEngine.SceneManagement;

namespace BeMyShotgunSir.Scripts.Core.Init
{
    public class InitSceneBootstrapper : SceneBootstrapper
    {
        protected override void Bootstrap()
        {
            if (SceneManager.GetSceneByName(SceneName.Lobby.ToString()).isLoaded)
                SceneManager.UnloadSceneAsync(SceneName.Lobby.ToString());

            // if (!SceneManager.GetSceneByName(SceneName.UI.ToString()).isLoaded)
            //     SceneManager.LoadScene(SceneName.UI.ToString(), LoadSceneMode.Additive);
        }
    }
}
