using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    /// <summary>
    /// Project-level adapter around FishNet scene APIs to keep naming conventions consistent
    /// without modifying package code.
    /// </summary>
    public sealed class FishNetSceneAdapter
    {
        public bool TryLoadGlobalScene(string sceneName, MonoBehaviour context, ReplaceOption replaceOption = ReplaceOption.OnlineOnly)
        {
            SceneManager fishNetSceneManager = InstanceFinder.SceneManager;
            if (fishNetSceneManager == null)
            {
                Debug.LogError("SceneCoordinator: FishNet SceneManager is not available.", context);
                return false;
            }

            SceneLoadData sceneLoadData = new(sceneName)
            {
                ReplaceScenes = replaceOption
            };

            fishNetSceneManager.LoadGlobalScenes(sceneLoadData);
            return true;
        }
    }
}
