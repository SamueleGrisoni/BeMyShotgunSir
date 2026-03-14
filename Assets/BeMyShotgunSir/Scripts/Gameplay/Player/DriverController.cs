using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    public class DriverController : MonoBehaviour
    {
        [SerializeField] private SOSidecarStats _stats;
        [SerializeField] private Rigidbody _sphere;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;

        private Input_Actions _inputActions;
        private Input_Actions.GameplayActions _gameplayActions;
        public bool IsDriftingButtonPressed => _gameplayActions.Drift.IsPressed();
        public float SteerInput => _gameplayActions.Steer.ReadValue<float>();
        public bool IsBoostButtonPressed => _gameplayActions.Boost.IsPressed();

        private IDrivingState _currentDrivingState;
        public readonly NormalDrivingState NormalState = new NormalDrivingState();
        public readonly DriftingDrivingState DriftingState = new DriftingDrivingState();
        public readonly AirDrivingState AirState = new AirDrivingState();

        public bool IsGrounded { get; private set; }
        public RaycastHit Hit { get; private set; }
        private List<float> _accelerationModifiers = new List<float>();
        private float CurrentAcceleration
        {
            get
            {
                float totalAcceleration = _stats.AccelerationForce;
                foreach (float mod in _accelerationModifiers)
                {
                    totalAcceleration += mod;
                }
                return totalAcceleration;
            }
        }
        public float DriftDirection { get; set; }

        private void Awake()
        {
            _inputActions = new Input_Actions();
            _inputActions.Enable();
            _gameplayActions = _inputActions.Gameplay;
        }
        private void Start() => ChangeState(NormalState);
        private void Update()
        {
            _currentDrivingState?.ExecuteUpdate(this);
            if (IsBoostButtonPressed)
            {
                ApplyBoost(_stats.BoostForce, _stats.BoostDuration);
            }
        }
        private void FixedUpdate()
        {
            _parent.position = _sphere.transform.position;
            CheckGround();
            _currentDrivingState?.ExecuteFixedUpdate(this);
        }
        public void ChangeState(IDrivingState state)
        {
            _currentDrivingState?.Exit(this);
            _currentDrivingState = state;
            _currentDrivingState?.Enter(this);
        }
        public RaycastHit CheckGround()
        {
            IsGrounded = Physics.Raycast(_sphere.position, -_parent.up, out RaycastHit hit, 0.6f);
            return hit;
        }
        public void ApplyAcceleration(Vector3 direction) => _sphere.AddForce(direction * CurrentAcceleration, ForceMode.Acceleration);
        public void ApplyGravity(float gravity) => _sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        public void ApplySteering(float steerAmount) => _parent.Rotate(_parent.up, steerAmount * _stats.SteeringForce * Time.fixedDeltaTime);
        public void ApplyLateralGrip(Vector3 direction)
        {
            Vector3 currentVelocity = _sphere.linearVelocity;
            Vector3 targetVelocity = direction * currentVelocity.magnitude;
            _sphere.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, _stats.LateralGripFactor * Time.fixedDeltaTime);
        }
        public void AnimateSidecar(Quaternion targetRot)
        {
            _sidecar.localRotation = Quaternion.Slerp(
                _sidecar.localRotation,
                targetRot,
                _stats.SteerAngularRotationSlerp
            );
        }
        private void ApplyBoost(float amount, float duration) => StartCoroutine(BoostRoutine(amount, duration));
        private IEnumerator BoostRoutine(float amount, float duration)
        {
            _accelerationModifiers.Add(amount);
            yield return new WaitForSeconds(duration);
            _accelerationModifiers.Remove(amount);
        }
        public Transform ParentTransform => _parent;
        public Transform SidecarTransform => _sidecar;
        public SOSidecarStats Stats => _stats;
    }
}