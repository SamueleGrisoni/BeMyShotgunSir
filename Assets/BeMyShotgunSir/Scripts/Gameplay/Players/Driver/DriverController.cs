using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Gameplay.Player.Driver.DriftingStates;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public class DriverController : NetworkBehaviour, IDriverControllerContext
    {
        [SerializeField] private SOSidecarStats _stats;
        [SerializeField] private Rigidbody _sphere;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;
        [SerializeField] private Collider _collider;

        [SerializeField] private Transform[] _wheelBones;
        [SerializeField] private Transform _handlebarBones;
        [SerializeField] private float _wheelRadius;
        private Quaternion _parentRotation;
        private Quaternion _sidecarLocalRotation;
        private IDrivingState _currentDrivingState;
        private IDrivingState _idleState = new IdleDrivingState();
        private IDrivingState _normalState = new NormalDrivingState();
        private IDrivingState _driftingState = new DriftingDrivingState();
        private IDrivingState _airState = new AirDrivingState();
        private IDrivingState _boostState = new BoostDrivingState();
        private float _currentMaxSpeed;
        private bool _isGrounded;
        private float _driftDirection;
        private float _currentBatteryCharge;
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
        private float _steerInput;
        private bool _isDriftButtonPressed;
        private bool _isBoostButtonPressed;
        private float _steerInputOnServer;
        private bool _isStartButtonPressed;
        private bool _isDrifiButtonPressedOnServer;
        private bool _isBoostButtonpressedOnServer;
        private bool _isStartButtonPressedOnServer;
        public override void OnStartServer()
        {
            base.OnStartServer();
            _currentDrivingState = _idleState;
        }
        public override void OnStartClient()
        {
            base.OnStartClient();

            _parent.gameObject.SetActive(true);

            if (IsOwner)
            {
                GetComponent<PlayerInput>().enabled = true;
            }
        }
        private void Awake()
        {
            Debug.Assert(_stats != null, "Missing Reference");
            Debug.Assert(_sphere != null, "Missing Reference");
            Debug.Assert(_parent != null, "Missing Reference");
            Debug.Assert(_sidecar != null, "Missing Reference");

            _currentMaxSpeed = _stats.MaxSpeed;
            _parentRotation = _parent.rotation;
            _sidecarLocalRotation = _sidecar.localRotation;
        }
        private void Update()
        {
            if (!IsServerInitialized) return;
            _currentDrivingState?.ExecuteUpdate(this);

        }
        private void LateUpdate()
        {
            _parent.position = _sphere.transform.position; // TODO maybe this is useless

            if (!IsServerInitialized) return;
            _parent.rotation = _parentRotation;
            _sidecar.localRotation = _sidecarLocalRotation;
        }
        private void FixedUpdate()
        {
            if (!IsServerInitialized) return;
            CheckGround();
            _currentDrivingState?.ExecuteFixedUpdate(this);
        }
        Transform IDriverControllerContext.ParentTransform => _parent;
        Transform IDriverControllerContext.SidecarTransform => _sidecar;
        Vector3 IDriverControllerContext.ParentForward => _parentRotation * Vector3.forward;
        Vector3 IDriverControllerContext.SidecarForward => (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
        SOSidecarStats IDriverControllerContext.Stats => _stats;
        IDrivingState IDriverControllerContext.IdleState => _idleState;
        IDrivingState IDriverControllerContext.NormalState => _normalState;
        IDrivingState IDriverControllerContext.DriftingState => _driftingState;
        IDrivingState IDriverControllerContext.AirState => _airState;
        IDrivingState IDriverControllerContext.BoostState => _boostState;
        float IDriverControllerContext.CurrentMaxSpeed => _currentMaxSpeed;
        bool IDriverControllerContext.IsGrounded => _isGrounded;
        bool IDriverControllerContext.IsDriftingButtonPressed => _isDrifiButtonPressedOnServer;
        float IDriverControllerContext.SteerInput => _steerInputOnServer;
        bool IDriverControllerContext.IsBoostButtonPressed => _isBoostButtonpressedOnServer;
        bool IDriverControllerContext.IsStartButtonPressed => _isStartButtonPressedOnServer;
        float IDriverControllerContext.DriftDirection
        {
            get => _driftDirection;
            set => _driftDirection = value;
        }
        float IDriverControllerContext.CurrentBatteryCharge
        {
            get => _currentBatteryCharge;
            set => _currentBatteryCharge = value;
        }
        void IDriverControllerContext.ChangeState(IDrivingState state) => ChangeState(state);
        void IDriverControllerContext.SetMaxSpeed(float maxSpeed) => _currentMaxSpeed = maxSpeed;
        void IDriverControllerContext.ApplyAcceleration(Vector3 direction)
        {
            _sphere.AddForce(direction * CurrentAcceleration, ForceMode.Acceleration);

            Vector3 currentVel = _sphere.linearVelocity;
            Vector3 horizontalVel = new(currentVel.x, 0, currentVel.z);
            if (horizontalVel.magnitude > _currentMaxSpeed)
            {
                Vector3 limitedHorizontalVel = horizontalVel.normalized * _currentMaxSpeed;
                _sphere.linearVelocity = new Vector3(limitedHorizontalVel.x, currentVel.y, limitedHorizontalVel.z);
            }
        }
        void IDriverControllerContext.ApplyGravity(float gravity) => _sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        void IDriverControllerContext.ApplySteering(float steerAmount)
        {
            _parent.Rotate(_parent.up, steerAmount * _stats.SteeringForce * Time.fixedDeltaTime);
            float steerAngle = steerAmount * _stats.SteeringForce * Time.fixedDeltaTime;
            var steerRotation = Quaternion.AngleAxis(steerAngle, _parent.up);
            _parentRotation *= steerRotation;
        }
        void IDriverControllerContext.ApplyVisualRotation(Quaternion targetRot)
        {
            // Questo metodo aggiunge una rotazione in più al sidecar rispetto al parent.
            // Serve per dare la sensazione visiva che il sidecar sta effettivamente ruotando.
            // Cambiare il valore di SteerAngularRotationSlerp per rendere più o meno veloce questa rotazione. Il valore è da settare meglio quando si ha i comandi touch
            _sidecarLocalRotation = Quaternion.Slerp(
                _sidecarLocalRotation,
                targetRot,
                _stats.SteerAngularRotationSlerp * Time.fixedDeltaTime // Cambiare SteerAngularRotationSlerp per rendere più o meno veloce la rotazione visiva del sidear
            );
        }
        void IDriverControllerContext.ApplyLateralGrip()
        {
            /*
            Vector3 direction = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
            Vector3 targetVelocity = direction * _sphere.linearVelocity.magnitude;
            _sphere.linearVelocity = Vector3.Lerp(_sphere.linearVelocity, targetVelocity, _stats.LateralGripFactor * Time.fixedDeltaTime); // TODO Probabilmente è da alzare molto LateralGripFactor per rendere meno sviloso il sidecar quando si sterza in NormalState 
            */

            // TODO REFACTORARE
            Vector3 forwardDir = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
            Vector3 flatForwardDir = new Vector3(forwardDir.x, 0, forwardDir.z).normalized;

            Vector3 currentVel = _sphere.linearVelocity;
            Vector3 horizontalVel = new(currentVel.x, 0, currentVel.z);
            Vector3 targetHorizontalVel = flatForwardDir * horizontalVel.magnitude;
            var newHorizontalVel = Vector3.Lerp(horizontalVel, targetHorizontalVel, _stats.LateralGripFactor * Time.fixedDeltaTime);
            _sphere.linearVelocity = new(newHorizontalVel.x, currentVel.y, newHorizontalVel.z);
        }
        void IDriverControllerContext.AnimateSidecar()
        {
            /*
            _sidecar.localRotation = Quaternion.Slerp(
                _sidecar.localRotation,
                targetRot,
                _stats.SteerAngularRotationSlerp
            );
            */
        }
        void IDriverControllerContext.ApplyBoost(float amount, float duration)
        {
            //StartCoroutine(BoostRoutine(amount, duration));
        }

        private void ChangeState(IDrivingState state)
        {
            _currentDrivingState?.Exit(this);
            _currentDrivingState = state;
            _currentDrivingState?.Enter(this);
        }

        private void CheckGround() => _isGrounded = Physics.Raycast(_sphere.position, -_parent.up, out RaycastHit hit, 0.6f);
        /*
        private IEnumerator BoostRoutine(float amount, float duration)
        {
            _accelerationModifiers.Add(amount);
            yield return new WaitForSeconds(duration);
            _accelerationModifiers.Remove(amount);
        }
        */
        [Client]
        private void OnSteer(InputValue value)
        {
            _steerInput = value.Get<float>();
            SyncSteerInput(_steerInput);
        }
        [Client]
        private void OnDrift(InputValue value)
        {
            _isDriftButtonPressed = value.isPressed;
            SyncDriftInput(_isDriftButtonPressed);
        }
        [Client]
        private void OnBoost(InputValue value)
        {
            _isBoostButtonPressed = value.isPressed;
            SyncBoostInput(_isBoostButtonPressed);
        }
        [Client]
        private void OnStart(InputValue value)
        {
            _isStartButtonPressed = value.isPressed;
            SyncStartInput(_isStartButtonPressed);
        }
        [ServerRpc]
        private void SyncSteerInput(float steerInput) => _steerInputOnServer = steerInput;
        [ServerRpc]
        private void SyncDriftInput(bool isDriftingButtonPressed) => _isDrifiButtonPressedOnServer = isDriftingButtonPressed;
        [ServerRpc]
        private void SyncBoostInput(bool isBoostButtonPressed) => _isBoostButtonpressedOnServer = isBoostButtonPressed;
        [ServerRpc]
        private void SyncStartInput(bool isStartButtonPressed) => _isStartButtonPressedOnServer = isStartButtonPressed;
    }
}
