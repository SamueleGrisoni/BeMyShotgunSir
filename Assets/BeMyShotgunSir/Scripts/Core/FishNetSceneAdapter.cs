using System;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Utils;
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
        public static event Action<SceneName> OnSceneLoaded;
        public static event Action<SceneName> OnSceneUnloaded;
        public static event Action<SceneName> OnSceneInitialized;

        private SceneManager _fishNetSceneManager;
        private bool _isInitialized = false;
        private void Initialize()
        {
            _fishNetSceneManager = InstanceFinder.SceneManager;
            _fishNetSceneManager.OnLoadEnd += HandleSceneLoadEnd;
            _fishNetSceneManager.OnUnloadEnd += HandleSceneUnloadEnd;

            LobbySceneBootstrapper.OnLobbySceneInitialized += HandleLobbySceneInitialized;
            RaceSceneBootstrapper.OnRaceSceneInitialized += HandleRaceSceneInitialized;
            _isInitialized = true;
        }

        private void HandleLobbySceneInitialized() =>
            OnSceneInitialized?.Invoke(SceneName.Lobby);
        private void HandleRaceSceneInitialized() =>
            OnSceneInitialized?.Invoke(SceneName.Race);

        private void HandleSceneUnloadEnd(SceneUnloadEndEventArgs args) //TODO test it
        {
            if (args.UnloadedScenesV2.Count > 0)
            {
                string sceneName = args.UnloadedScenesV2.ToArray()[0].Name;
                if (Enum.TryParse(sceneName, out SceneName unloadedScene))
                    OnSceneUnloaded?.Invoke(unloadedScene);
                else
                    Log.ELazy(() => $"Unloaded scene '{sceneName}' does not match any known SceneName enum values.", this);
            }
        }

        private void HandleSceneLoadEnd(SceneLoadEndEventArgs args)
        {
            if (args.LoadedScenes.Length > 0)
            {
                string sceneName = args.LoadedScenes[0].name;
                if (Enum.TryParse(sceneName, out SceneName loadedScene))

                    OnSceneLoaded?.Invoke(loadedScene);
                else
                    Log.ELazy(() => $"Loaded scene '{sceneName}' does not match any known SceneName enum values.", this);
            }
        }

        public bool TryLoadGlobalScene(string sceneName, MonoBehaviour context, ReplaceOption replaceOption = ReplaceOption.OnlineOnly)
        {
            if (!_isInitialized)
                Initialize();
            if (_fishNetSceneManager == null)
            {
                Log.ELazy(() => "FishNet SceneManager is not available.", context);
                return false;
            }

            SceneLoadData sceneLoadData = new(sceneName)
            {
                ReplaceScenes = replaceOption
            };

            _fishNetSceneManager.LoadGlobalScenes(sceneLoadData);
            return true;
        }
    }
}

