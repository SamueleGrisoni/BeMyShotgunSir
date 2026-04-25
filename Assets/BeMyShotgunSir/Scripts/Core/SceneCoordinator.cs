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
        Track1 = 3,
        UI = 4
    }
    public class SceneCoordinator : MonoBehaviour
    {
        public readonly FishNetSceneAdapter FishnetSceneManager = new FishNetSceneAdapter();
        public void LoadInitScene()
        {
            if (UnitySceneManager.GetSceneByName(SceneName.Init.ToString()).isLoaded)
                return;
            UnitySceneManager.LoadScene(SceneName.Init.ToString(), UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
        public void LoadLobby() =>
            FishnetSceneManager.TryLoadGlobalScene(SceneName.Lobby.ToString(), this, ReplaceOption.OnlineOnly);
    }
}
