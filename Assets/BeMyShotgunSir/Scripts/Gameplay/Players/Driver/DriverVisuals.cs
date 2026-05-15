using System.Collections;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Audio;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.Players.Driver
{
    public class DriverVisuals : NetworkBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private DriverStats _stats;
        [SerializeField] private MovementController _movement;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;
        [SerializeField] public Transform _visualModel;

        [Header("Smoothing on server")]
        [SerializeField] private float _smoothingSpeedFast = 100f;

        [Header("Snapshot Interpolation")]
        [SerializeField] private float _teleportThreshold = 3f;
        //[SerializeField] private float _smoothingSpeedSlow = 15f;

        [Header("Visual Effects")]
        [SerializeField] private Transform _handle;
        [SerializeField] private List<ParticleSystem> _driftParticles = new List<ParticleSystem>();
        [SerializeField] private List<TrailRenderer> _trailRendereresDrift = new List<TrailRenderer>();
        [SerializeField] private List<ParticleSystem> _boostParticles = new List<ParticleSystem>();
        [SerializeField] private List<ParticleSystem> _armorParticles = new List<ParticleSystem>();

        private struct TransformSnapshot
        {
            public Vector3 Position;
            public Quaternion ParentRotation;
            public Quaternion SidecarLocalRotation;
        }

        private TransformSnapshot _prevSnapshot;
        private TransformSnapshot _nextSnapshot;
        private double _prevSnapshotTime;
        private double _nextSnapshotTime;
        private bool _hasFirstSnapshot;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TimeManager.OnPostTick += OnPostTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnPostTick -= OnPostTick;
        }

        private void OnPostTick()
        {
            if (IsOwner || IsServerInitialized) return;

            _prevSnapshot = _nextSnapshot;
            _prevSnapshotTime = _nextSnapshotTime;

            _nextSnapshot = new TransformSnapshot
            {
                Position = _movement.MovementPosition,
                ParentRotation = _movement.ParentRotation,
                SidecarLocalRotation = _movement.SidecarLocalRotation,
            };
            _nextSnapshotTime = TimeManager.TicksToTime(TimeManager.LocalTick);
            _hasFirstSnapshot = true;
        }

        private void LateUpdate()
        {

            if (IsServerInitialized)
            {
                _parent.position = Vector3.Lerp(_parent.position, _movement.MovementPosition, Time.deltaTime * _smoothingSpeedFast);
                _parent.rotation = Quaternion.Slerp(_parent.rotation, _movement.ParentRotation, Time.deltaTime * _smoothingSpeedFast);
                _sidecar.localRotation = Quaternion.Slerp(_sidecar.localRotation, _movement.SidecarLocalRotation, Time.deltaTime * _smoothingSpeedFast);
            }
            else if (IsOwner)
            {
                _parent.position = _movement.MovementPosition;
                _parent.rotation = _movement.ParentRotation;
                _sidecar.localRotation = _movement.SidecarLocalRotation;
            }
            else
            {
                ApplySnapshotInterpolation();
            }

            AnimateSteer();
            AnimateDrifting();
            AnimateBoost();
        }

        private void ApplySnapshotInterpolation()
        {
            if (!_hasFirstSnapshot)
            {
                _parent.position = _movement.MovementPosition;
                _parent.rotation = _movement.ParentRotation;
                _sidecar.localRotation = _movement.SidecarLocalRotation;
                return;
            }

            double tickDuration = TimeManager.TickDelta;
            double snapshotSpan = _nextSnapshotTime - _prevSnapshotTime;

            if (snapshotSpan <= 0.0)
            {
                _parent.position = _nextSnapshot.Position;
                _parent.rotation = _nextSnapshot.ParentRotation;
                _sidecar.localRotation = _nextSnapshot.SidecarLocalRotation;
                return;
            }

            double now = TimeManager.TicksToTime(TimeManager.LocalTick) + Time.deltaTime;
            float alpha = Mathf.Clamp01((float)((now - _prevSnapshotTime) / snapshotSpan));

            float dist = Vector3.Distance(_parent.position, _prevSnapshot.Position);
            if (dist > _teleportThreshold)
            {
                _parent.position = _nextSnapshot.Position;
                _parent.rotation = _nextSnapshot.ParentRotation;
                _sidecar.localRotation = _nextSnapshot.SidecarLocalRotation;
                return;
            }

            _parent.position = Vector3.Lerp(_prevSnapshot.Position, _nextSnapshot.Position, alpha);
            _parent.rotation = Quaternion.Slerp(_prevSnapshot.ParentRotation, _nextSnapshot.ParentRotation, alpha);
            _sidecar.localRotation = Quaternion.Slerp(_prevSnapshot.SidecarLocalRotation, _nextSnapshot.SidecarLocalRotation, alpha);
        }

        private void AnimateSteer()
        {
            _handle.localRotation = Quaternion.Slerp(
                _handle.localRotation,
                Quaternion.Euler(0, _movement.CurrentSteerInput * _stats.AnimationStats.MaxSteerAngle, 0),
                Time.deltaTime * _stats.AnimationStats.SteerAnimationSpeed
            );
        }
        private void AnimateDrifting()
        {
            bool isDrifting = _movement.IsDrifting();
            foreach (ParticleSystem p in _driftParticles)
            {
                ParticleSystem.EmissionModule emission = p.emission;
                emission.enabled = isDrifting;
            }
            foreach (TrailRenderer tr in _trailRendereresDrift)
            {
                tr.emitting = isDrifting;
            }
        }
        private void AnimateBoost()
        {
            if (!IsOwner) return;

            bool isBoosting = _movement.IsBoosting();
            foreach (ParticleSystem p in _boostParticles)
            {
                ParticleSystem.EmissionModule emission = p.emission;
                emission.enabled = isBoosting;
            }
        }
        public void OilAnimation()
        {
            GameServices.Instance.Channels.AudioRequestEvent.RaiseEvent(null, new AudioRequest(RequestEnum.OilSlip), _audioSource);
            StartCoroutine(ExecuteOilAnimation());
        }

        private IEnumerator ExecuteOilAnimation()
        {
            float timer = 0f;
            Quaternion startRotation = _visualModel.localRotation;

            while (timer < _stats.AnimationStats.OilAnimationDuration)
            {
                timer += Time.deltaTime;
                float currentRotation = (_stats.AnimationStats.OilTotalRotation / _stats.AnimationStats.OilAnimationDuration) * Time.deltaTime;
                _visualModel.localRotation *= Quaternion.Euler(0, 0, currentRotation);
                yield return null;
            }

            _visualModel.localRotation = startRotation;
        }

        public void SetArmorVisualEffects(bool isActive)
        {
            foreach (ParticleSystem p in _armorParticles)
            {
                ParticleSystem.EmissionModule emission = p.emission;
                emission.enabled = isActive;
            }
        }
    }
}
