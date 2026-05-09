using System;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceSceneBootstrapperInitializer
    {
        void Initialize(IRaceManager_Bootstrapper raceManager);
    }
    public class RaceSceneBootstrapper : SceneBootstrapper, IRaceSceneBootstrapperInitializer
    {
        private bool _isInitialized = false;
        public static event Action<IRaceSceneBootstrapperInitializer> OnRaceBootStrapperAwakened;
        public static event Action OnRaceSceneInitialized;

        [SerializeField] private InterfaceSerializer<RaceBindTarget, IRaceBindTarget>[] _bindTargets;
        private IRaceBindTarget[] _coercedTargets;
        private IRaceManager_Bootstrapper _raceManager;

        private void Awake() =>
                 OnRaceBootStrapperAwakened?.Invoke(this);

        private void OnEnable()
        {
            if (!_isInitialized)
                RaceManager.OnRaceManagerInitialized += Initialize;
        }

        public void Initialize(IRaceManager_Bootstrapper raceManager)
        {
            if (_isInitialized)
                return;
            _raceManager = raceManager;
            Bootstrap();
            _isInitialized = true;
            RaceManager.OnRaceManagerInitialized -= Initialize;
        }

        protected override void Bootstrap()
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
            Bind();
        }

        private void Bind()
        {
            if (_coercedTargets == null)
            {
                Log.ELazy(() => "Coerced bind targets are null. Cannot bind race.", this);
                return;
            }
            _raceManager.BindRace_Initial(_coercedTargets);
            OnRaceSceneInitialized?.Invoke();
        }
    }
}
