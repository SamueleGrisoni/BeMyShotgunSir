using System;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceSceneBootstrapper : SceneBootstrapper
    {
        /// <summary>
        /// Avoid subscribing to this event directly. Instead, subscribe to FishNetSceneAdapter.OnSceneInitialized and check for SceneName to determine when a scene is initialized.
        /// </summary>
        public static event Action OnRaceSceneInitialized;

        [SerializeField] private InterfaceSerializer<RaceBindTarget, IRaceBindTarget>[] _bindTargets;
        private IRaceBindTarget[] _coercedTargets;

        private void OnDisable() =>
            RaceManager.OnRaceManagerInitialized -= OnRaceManagerReady;

        private void Bind(IRaceManager_Bootstrapper manager)
        {
            if (_coercedTargets == null)
            {
                Log.ELazy(() => "Coerced bind targets are null. Cannot bind race.", this);
                return;
            }
            manager.BindRace_Initial(_coercedTargets);
            OnRaceSceneInitialized?.Invoke();
        }

        private void OnRaceManagerReady(IRaceManager manager)
        {
            if (manager is not IRaceManager_Bootstrapper manager_Bootstrapper)
                return;
            TryInitialize();
            Bind(manager_Bootstrapper);
        }

        protected override void Initialize()
        {
            _coercedTargets = new IRaceBindTarget[_bindTargets.Length];
            for (int i = 0; i < _bindTargets.Length; i++)
            {
                if (_bindTargets[i].Interface == null)
                {
                    Log.ELazy(() => $"Bind target at index {i} does not implement IRaceBindTarget. Skipping.", this);
                    continue;
                }
                _coercedTargets[i] = _bindTargets[i].Interface;
            }
            _suppressAutoInitialize = true;

            var raceManager = GameServices.Instance.RaceManager as IRaceManager_Bootstrapper;
            if (raceManager == null)
                RaceManager.OnRaceManagerInitialized += OnRaceManagerReady;
            else
                Bind(raceManager);
        }
    }
}
