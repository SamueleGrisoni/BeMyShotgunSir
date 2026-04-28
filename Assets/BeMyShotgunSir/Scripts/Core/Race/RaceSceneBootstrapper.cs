using System;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceSceneBootstrapper : SceneBootstrapper
    {
        public static event Action OnRaceSceneInitialized;

        [SerializeField] private InterfaceSerializer<RaceBindTarget, IRaceBindTarget>[] _bindTargets;
        private IRaceBindTarget[] _coercedTargets;

        private void OnDisable() =>
            RaceManager.OnRaceManagerSpawned -= OnRaceManagerSpawned;

        private void Bind(RaceManager manager)
        {
            if (_coercedTargets == null)
            {
                Log.ELazy(() => "RaceSceneBootstrapper: Coerced bind targets are null. Cannot bind race.", this);
                return;
            }
            manager.BindLobby(_coercedTargets);
            OnRaceSceneInitialized?.Invoke();
        }

        private void OnRaceManagerSpawned(RaceManager manager)
        {
            TryInitialize();
            Bind(manager);
        }

        protected override void Initialize()
        {
            //NOTE this should be handled by fishnet (scenes are now networked)
            // UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneName.Lobby.ToString());

            _coercedTargets = new IRaceBindTarget[_bindTargets.Length];
            for (int i = 0; i < _bindTargets.Length; i++)
            {
                if (_bindTargets[i].Interface == null)
                {
                    Log.ELazy(() => $"RaceSceneInitializer: Bind target at index {i} does not implement IRaceBindTarget. Skipping.", this);
                    continue;
                }
                _coercedTargets[i] = _bindTargets[i].Interface;
            }
            _suppressAutoInitialize = true;

            RaceManager raceManager = GameServices.Instance.RaceManager;
            if (raceManager == null)
                RaceManager.OnRaceManagerSpawned += OnRaceManagerSpawned;
            else
                Bind(raceManager);
        }
    }
}
