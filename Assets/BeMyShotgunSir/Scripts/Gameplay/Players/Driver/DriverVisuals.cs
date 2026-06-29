using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using FishNet.Object;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public class DriverVisuals : NetworkBehaviour
    {
        [Header("Audio")]
        [SerializeField] private EventReference _engineEvent;
        [SerializeField] private EventReference _driftEvent;
        [SerializeField] private EventReference _collisionEvent;
        [SerializeField] private EventReference _oilEvent;
        [SerializeField] private EventReference _armorCollisionEvent;
        [SerializeField] private EventReference _barrierCollisionEvent;
        private EventInstance _engineInstance;
        private EventInstance _driftIstance;
        private EventInstance _collisionInstance;

        [SerializeField] private DriverStats _stats;
        [SerializeField] private MovementController _movement;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;
        [SerializeField] public Transform _visualModel;

        [Header("Smoothing on server")]
        [SerializeField] private float _smoothingSpeedFast = 100f;

        [Header("Snapshot Interpolation")]
        [SerializeField] private float _teleportThreshold = 3f;

        [Header("Visual Effects")]
        [SerializeField] private Transform _handle;
        [SerializeField] private List<ParticleSystem> _driftParticles = new List<ParticleSystem>();
        [SerializeField] private List<TrailRenderer> _trailRendereresDrift = new List<TrailRenderer>();
        [SerializeField] private List<ParticleSystem> _boostParticles = new List<ParticleSystem>();
        [SerializeField] private List<ParticleSystem> _shieldParticles = new List<ParticleSystem>();
        [SerializeField] private SkinnedMeshRenderer _overlayMeshRenderer;
        [SerializeField] private GameObject _aimDecalProjector;

        [SerializeField] private Animator _visualModelAnimator;

        private Material _armorOverlayMaterial;


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

            if (_overlayMeshRenderer.materials.Length > 1)
            {
                _armorOverlayMaterial = _overlayMeshRenderer.materials[1];
                _armorOverlayMaterial.SetFloat("_OverlayAlpha", 0.0f);
            }

            if (_aimDecalProjector != null)
                _aimDecalProjector.SetActive(false);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _engineInstance = RuntimeManager.CreateInstance(_engineEvent);
            _engineInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            _engineInstance.start();

            _driftIstance = RuntimeManager.CreateInstance(_driftEvent);
            _driftIstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));

            _collisionInstance = RuntimeManager.CreateInstance(_collisionEvent);
            _collisionInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnPostTick -= OnPostTick;
            if (_engineInstance.isValid())
            {
                _engineInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _engineInstance.release();
            }
            if (_driftIstance.isValid())
            {
                _driftIstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _driftIstance.release();
            }
            if (_collisionInstance.isValid())
            {
                _collisionInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _collisionInstance.release();
            }
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
            _parent.position = Vector3.Lerp(_parent.position, _movement.MovementPosition, Time.deltaTime * _smoothingSpeedFast);
            _parent.rotation = Quaternion.Slerp(_parent.rotation, _movement.ParentRotation, Time.deltaTime * _smoothingSpeedFast);
            _sidecar.localRotation = Quaternion.Slerp(_sidecar.localRotation, _movement.SidecarLocalRotation, Time.deltaTime * _smoothingSpeedFast);
            /*
            if (IsServerInitialized)
            {
                _parent.position = _movement.MovementPosition;
                _parent.rotation = _movement.ParentRotation;
                _sidecar.localRotation = _movement.SidecarLocalRotation;
            }
            else
            {
                _parent.position = Vector3.Lerp(_parent.position, _movement.MovementPosition, Time.deltaTime * _smoothingSpeedFast);
                _parent.rotation = Quaternion.Slerp(_parent.rotation, _movement.ParentRotation, Time.deltaTime * _smoothingSpeedFast);
                _sidecar.localRotation = Quaternion.Slerp(_sidecar.localRotation, _movement.SidecarLocalRotation, Time.deltaTime * _smoothingSpeedFast);
            }
            */
            /*
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
            else if (!IsOwner && _movement.ShotgunPartOfTeam())
            {
                _parent.position = Vector3.Lerp(_parent.position, _movement.MovementPosition, Time.deltaTime * 10f);
                _parent.rotation = Quaternion.Slerp(_parent.rotation, _movement.ParentRotation, Time.deltaTime * 10f);
                _sidecar.localRotation = Quaternion.Slerp(_sidecar.localRotation, _movement.SidecarLocalRotation, Time.deltaTime * 10f);
            }
            else
            {
                ApplySnapshotInterpolation();
            }
            */

            AnimateSteer();
            AnimateDrifting();
            AnimateBoost();
            EngineSound();
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

            _parent.position = Vector3.LerpUnclamped(_prevSnapshot.Position, _nextSnapshot.Position, alpha);
            _parent.rotation = Quaternion.LerpUnclamped(_prevSnapshot.ParentRotation, _nextSnapshot.ParentRotation, alpha);
            _sidecar.localRotation = Quaternion.LerpUnclamped(_prevSnapshot.SidecarLocalRotation, _nextSnapshot.SidecarLocalRotation, alpha);
        }

        private void EngineSound()
        {
            if (!_movement.ShotgunPartOfTeam() && !IsOwner) return;

            if (_engineInstance.isValid())
            {
                _engineInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                _engineInstance.setParameterByName("Speed", _movement.GetCurrentVelocity());
            }
        }

        private void AnimateSteer()
        {
            _handle.localRotation = Quaternion.Slerp(
                _handle.localRotation,
                Quaternion.Euler(-21.476f, _movement.CurrentSteerInput * _stats.AnimationStats.MaxSteerAngle, 0),
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

            if (!_movement.ShotgunPartOfTeam() && !IsOwner) return;

            _driftIstance.getPlaybackState(out PLAYBACK_STATE playbackState);
            if (isDrifting)
            {
                if (playbackState == PLAYBACK_STATE.STOPPED)
                    _driftIstance.start();
            }
            else
            {
                if (playbackState != PLAYBACK_STATE.STOPPED)
                    _driftIstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
        private void AnimateBoost()
        {
            if (!_movement.ShotgunPartOfTeam() && !IsOwner) return;

            bool isBoosting = _movement.IsBoosting();
            foreach (ParticleSystem p in _boostParticles)
            {
                ParticleSystem.EmissionModule emission = p.emission;
                emission.enabled = isBoosting;
            }

            // SOUND
        }
        public void OilAnimation()
        {
            if (_visualModelAnimator != null)
            {
                _visualModelAnimator.SetTrigger("Oil");
            }

            if (!_movement.ShotgunPartOfTeam() && !IsOwner) return;

            RuntimeManager.PlayOneShotAttached(_oilEvent, gameObject);
        }

        public void SetArmorVisualEffects(bool isActive)
        {
            if (_armorOverlayMaterial != null)
            {
                _armorOverlayMaterial.SetFloat("_OverlayAlpha", isActive ? 1.0f : 0.0f);
            }
        }

        public void SetAimVisualEffects(bool isActive)
        {
            if (_aimDecalProjector != null)
                _aimDecalProjector.SetActive(isActive);
        }

        public void SetShieldVisualEffects(bool isActive)
        {
            foreach (ParticleSystem p in _shieldParticles)
            {
                ParticleSystem.EmissionModule emission = p.emission;
                emission.enabled = isActive;
            }
        }

        public void RightCollisionAnimation()
        {
            if (_visualModelAnimator != null)
                _visualModelAnimator.SetTrigger("HitRight");

            RuntimeManager.PlayOneShotAttached(_collisionEvent, gameObject);
        }

        public void LeftCollisionAnimation()
        {
            if (_visualModelAnimator != null)
                _visualModelAnimator.SetTrigger("HitLeft");

            RuntimeManager.PlayOneShotAttached(_collisionEvent, gameObject);
        }

        public void BumpSoundEffect()
        {
            RuntimeManager.PlayOneShotAttached(_barrierCollisionEvent, gameObject);
        }
        public void ArmorCollisionSuondEffect()
        {
            RuntimeManager.PlayOneShotAttached(_armorCollisionEvent, gameObject);
        }
    }
}
