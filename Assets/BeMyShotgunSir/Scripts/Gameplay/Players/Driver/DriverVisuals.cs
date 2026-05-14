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
        [SerializeField] private Transform _visualModel;
        [SerializeField] private float _smoothingSpeedFast = 100f;
        [SerializeField] private float _smoothingSpeedSlow = 15f;

        [SerializeField] private Transform _handle;
        [SerializeField] private List<ParticleSystem> _driftParticles = new List<ParticleSystem>();
        [SerializeField] private List<TrailRenderer> _trailRendereresDrift = new List<TrailRenderer>();
        [SerializeField] private List<ParticleSystem> _boostParticles = new List<ParticleSystem>();

        private void LateUpdate()
        {
            //_parent.position = _movement.MovementPosition;
            //_parent.rotation = _movement.ParentRotation;
            //_sidecar.localRotation = _movement.SidecarLocalRotation;

            if (IsServerInitialized)
            {
                _parent.position = Vector3.Lerp(_parent.position, _movement.MovementPosition, Time.deltaTime * _smoothingSpeedFast);
                _parent.rotation = Quaternion.Slerp(_parent.rotation, _movement.ParentRotation, Time.deltaTime * _smoothingSpeedFast);
                _sidecar.localRotation = Quaternion.Slerp(_sidecar.localRotation, _movement.SidecarLocalRotation, Time.deltaTime * _smoothingSpeedFast);
            }
            else
            {
                _parent.position = Vector3.Lerp(_parent.position, _movement.MovementPosition, Time.deltaTime * _smoothingSpeedSlow);
                _parent.rotation = Quaternion.Slerp(_parent.rotation, _movement.ParentRotation, Time.deltaTime * _smoothingSpeedSlow);
                _sidecar.localRotation = Quaternion.Slerp(_sidecar.localRotation, _movement.SidecarLocalRotation, Time.deltaTime * _smoothingSpeedSlow);
            }

            AnimateSteer();
            AnimateDrifting();
            AnimateBoost();
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
    }
}
