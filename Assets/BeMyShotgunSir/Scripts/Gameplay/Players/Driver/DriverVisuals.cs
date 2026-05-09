using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.Players.Driver
{
    public class DriverVisuals : MonoBehaviour
    {
        [SerializeField] private DriverStats _stats;
        [SerializeField] private MovementController _movement;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;
        [SerializeField] private Transform _visualModel;
        [SerializeField] private float _smoothingSpeed = 100f;

        [SerializeField] private Transform _handle;

        private void LateUpdate()
        {
            if (_movement == null) return;

            // _parent.position = _movement.MovementPosition;
            // _parent.rotation = _movement.ParentRotation;
            // _sidecar.localRotation = _movement.SidecarLocalRotation;

            _parent.position = Vector3.Lerp(_parent.position, _movement.MovementPosition, Time.deltaTime * _smoothingSpeed);
            _parent.rotation = Quaternion.Slerp(_parent.rotation, _movement.ParentRotation, Time.deltaTime * _smoothingSpeed);
            _sidecar.localRotation = Quaternion.Slerp(_sidecar.localRotation, _movement.SidecarLocalRotation, Time.deltaTime * _smoothingSpeed);

            AnimateSteer();
        }

        private void AnimateSteer()
        {
            _handle.localRotation = Quaternion.Slerp(
                _handle.localRotation,
                Quaternion.Euler(0, _movement.CurrentSteerInput * _stats.AnimationStats.MaxSteerAngle, 0),
                Time.deltaTime * _stats.AnimationStats.SteerAnimationSpeed
            );
        }
    }
}
