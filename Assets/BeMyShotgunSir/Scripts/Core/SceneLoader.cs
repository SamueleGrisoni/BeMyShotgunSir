using FishNet;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using FishNet.Managing.Scened;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    public enum SceneName
    {
        Persistent = 0,
        Init = 1,
        Lobby = 2,
        Track1 = 3
    }
    public class SceneLoader : MonoBehaviour
    {
        private SceneManager _sceneManager;
        private void Start() => _sceneManager = InstanceFinder.SceneManager;
        public void LoadInitMenuScene()
        {
            if (UnitySceneManager.GetSceneByName(SceneName.Init.ToString()).isLoaded)
                return;
            UnitySceneManager.LoadScene(SceneName.Init.ToString(), UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
        public void LoadLobbyScene()
        {
            SceneLoadData sld = new(SceneName.Lobby.ToString()) { ReplaceScenes = ReplaceOption.OnlineOnly };
            _sceneManager.LoadGlobalScenes(sld);
        }
    }
}
