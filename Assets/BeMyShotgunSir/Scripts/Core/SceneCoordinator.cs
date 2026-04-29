using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using FishNet.Managing.Scened;
using UnityEngine;
using BeMyShotgunSir.Scripts.Utils;

namespace BeMyShotgunSir.Scripts.Core
{
    public enum SceneName
    {
        Persistent = 0,
        Init = 1,
        Lobby = 2,
        Race = 3
    }

    public class SceneCoordinator : MonoBehaviour
    {
        public readonly FishNetSceneAdapter FishnetSceneManager = new FishNetSceneAdapter();

        public void LoadInitScene()
        {
            if (UnitySceneManager.GetSceneByName(SceneName.Init.ToString()).isLoaded)
                return;
            UnitySceneManager.LoadScene(SceneName.Init.ToString(), UnityEngine.SceneManagement.LoadSceneMode.Additive);
            Log.DLazy(() => "Loading Init scene.", this);
        }

        public void LoadLobbyScene()
        {
            FishnetSceneManager.TryLoadGlobalScene(SceneName.Lobby.ToString(), this, ReplaceOption.OnlineOnly);
            Log.DLazy(() => "Loading Lobby scene.", this);
        }
        internal void LoadRaceScene()
        {
            FishnetSceneManager.TryLoadGlobalScene(SceneName.Race.ToString(), this, ReplaceOption.OnlineOnly);
            Log.DLazy(() => "Loading Race scene.", this);
        }
    }
}
