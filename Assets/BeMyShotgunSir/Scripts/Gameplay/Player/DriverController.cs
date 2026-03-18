using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    public class DriverController : MonoBehaviour, IDriverControllerContext
    {
        [SerializeField] private SOSidecarStats _stats;
        [SerializeField] private Rigidbody _sphere;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;

        [SerializeField] private Transform[] _wheelBones;
        [SerializeField] private Transform _handlebarBones;
        [SerializeField] private float _wheelRadius;
        private Input_Actions _inputActions;
        private Input_Actions.GameplayActions _gameplayActions;
        private IDrivingState _currentDrivingState = new NormalDrivingState();
        private IDrivingState _normalState = new NormalDrivingState();
        private IDrivingState _driftingState = new DriftingDrivingState();
        private IDrivingState _airState = new DriftingDrivingState();
        private bool _isGrounded;
        private float _driftDirection;
        private List<float> _accelerationModifiers = new();
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
        private void Awake()
        {
            Debug.Assert(_stats != null, "Missing Reference");
            Debug.Assert(_sphere != null, "Missing Reference");
            Debug.Assert(_parent != null, "Missing Reference");
            Debug.Assert(_sidecar != null, "Missing Reference");
            _inputActions = new Input_Actions();
            _inputActions.Enable();
            _gameplayActions = _inputActions.Gameplay;
        }
        private void Update() => _currentDrivingState?.ExecuteUpdate(this);
        private void FixedUpdate()
        {
            _parent.position = _sphere.transform.position;
            CheckGround();
            _currentDrivingState?.ExecuteFixedUpdate(this);
        }
        Transform IDriverControllerContext.ParentTransform => _parent;
        Transform IDriverControllerContext.SidecarTransform => _sidecar;
        SOSidecarStats IDriverControllerContext.Stats => _stats;
        IDrivingState IDriverControllerContext.NormalState => _normalState;
        IDrivingState IDriverControllerContext.DriftingState => _driftingState;
        IDrivingState IDriverControllerContext.AirState => _airState;
        bool IDriverControllerContext.IsGrounded => _isGrounded;
        bool IDriverControllerContext.IsDriftingButtonPressed => _gameplayActions.Drift.IsPressed();
        float IDriverControllerContext.SteerInput => _gameplayActions.Steer.ReadValue<float>();
        bool IDriverControllerContext.IsBoostButtonPressed => _gameplayActions.Boost.IsPressed();
        float IDriverControllerContext.DriftDirection
        {
            get => _driftDirection;
            set => _driftDirection = value;
        }
        void IDriverControllerContext.ChangeState(IDrivingState state) => ChangeState(state);
        void IDriverControllerContext.ApplyAcceleration(Vector3 direction) => _sphere.AddForce(direction * CurrentAcceleration, ForceMode.Acceleration);
        void IDriverControllerContext.ApplyGravity(float gravity) => _sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        void IDriverControllerContext.ApplySteering(float steerAmount) => _parent.Rotate(_parent.up, steerAmount * _stats.SteeringForce * Time.fixedDeltaTime);
        void IDriverControllerContext.ApplyLateralGrip(Vector3 direction)
        {
            Vector3 currentVelocity = _sphere.linearVelocity;
            Vector3 targetVelocity = direction * currentVelocity.magnitude;
            _sphere.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, _stats.LateralGripFactor * Time.fixedDeltaTime);
        }
        void IDriverControllerContext.AnimateSidecar(Quaternion targetRot)
        {
            _sidecar.localRotation = Quaternion.Slerp(
                _sidecar.localRotation,
                targetRot,
                _stats.SteerAngularRotationSlerp
            );
        }
        void IDriverControllerContext.ApplyBoost(float amount, float duration) => StartCoroutine(BoostRoutine(amount, duration));

        private void ChangeState(IDrivingState state)
        {
            _currentDrivingState?.Exit(this);
            _currentDrivingState = state;
            _currentDrivingState?.Enter(this);
        }
        private void CheckGround() => _isGrounded = Physics.Raycast(_sphere.position, -_parent.up, out RaycastHit hit, 0.6f);
        private IEnumerator BoostRoutine(float amount, float duration)
        {
            _accelerationModifiers.Add(amount);
            yield return new WaitForSeconds(duration);
            _accelerationModifiers.Remove(amount);
        }
    }
}