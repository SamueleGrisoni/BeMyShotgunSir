using UnityEngine.SceneManagement;

namespace BeMyShotgunSir.Scripts.Core.Init
{
    public class InitSceneInitializer : SceneInitializer
    {
        protected override void Initializer()
        {
            if (SceneManager.GetSceneByName(SceneName.Lobby.ToString()).isLoaded)
                SceneManager.UnloadSceneAsync(SceneName.Lobby.ToString());
        }
    }
}
