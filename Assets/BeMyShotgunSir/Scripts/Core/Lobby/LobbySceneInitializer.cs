namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbySceneInitializer : SceneInitializer
    {
        protected override void Initializer() => UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneName.Init.ToString());
    }
}
