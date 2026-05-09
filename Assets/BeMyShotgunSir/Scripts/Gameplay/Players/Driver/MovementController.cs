using BeMyShotgunSir.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates;
using BeMyShotgunSir.Scripts.UI;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    #region Enums and Structs

    public enum GroundType : byte
    {
        Normal,
        Grass,
        Oil,
    }

    public enum DrivingStateTpye : byte
    {
        Idle,
        Normal,
        Drifting,
        Boost,
        Grass,
        Oil,
    }

    public struct ReplicateData : IReplicateData
    {
        public float SteerInput;
        public bool IsDrifting;
        public bool IsBoosting;
        public bool IsStarting;
        public ReplicateData(float steerInput, bool isDrifting, bool isBoosting, bool isStarting) : this()
        {
            SteerInput = steerInput;
            IsDrifting = isDrifting;
            IsBoosting = isBoosting;
            IsStarting = isStarting;
        }

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public PredictionRigidbody PredictionRigidbody;
        public float ParentRotationY; // TODO dato che la rotazione avviene solo su un asse, scremare
        public float SidecarLocalRotationY; // TODO dato che la rotezione avviene solo su un asse, scremare
        public Vector3 CurrentLinearVelocity; // TODO da togliere. Forse si può già usare quella del predictionRigidboy
        public float DriftDirection;
        public float CurrentBatteryCharge;
        public float BatteryChargeTimer;
        public float BoostTimer;
        public float OilAnimationTimer;
        public DrivingStateTpye StateType;
        public DrivingStateTpye PreviousStateType;
        public ReconcileData(PredictionRigidbody pr, float parentRotationY, float sidecarLocalRotationY, Vector3 currentLinearVelocity,
                                float driftDirection, float currentBatteryCharge, float batteryChargeTimer, float boostTimer, float oilAnimationTimer, DrivingStateTpye stateType, DrivingStateTpye previousStateType) : this()
        {
            PredictionRigidbody = pr;
            ParentRotationY = parentRotationY;
            SidecarLocalRotationY = sidecarLocalRotationY;
            CurrentLinearVelocity = currentLinearVelocity;
            DriftDirection = driftDirection;
            CurrentBatteryCharge = currentBatteryCharge;
            BatteryChargeTimer = batteryChargeTimer;
            BoostTimer = boostTimer;
            OilAnimationTimer = oilAnimationTimer;
            StateType = stateType;
            PreviousStateType = previousStateType;
        }
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    #endregion

    public class MovementController : NetworkBehaviour, IDrivingStateContext
    {
        [SerializeField] private DriverController _driverController;
        public int? TeamId => _driverController == null ? null : _driverController.TeamId;
        public IDriverInputConsumer InputConsumer => _driverController != null ? _driverController._inputConsumer : null;

        [SerializeField] private DriverInput _input;
        [SerializeField] private DriverStats _stats;

        [SerializeField] private Rigidbody _movement;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;
        [SerializeField] private Transform _sidecarModel;

        [SerializeField] private LayerMask _sidecarLayerMask;
        [SerializeField] private float _bumpRadius;
        [SerializeField] private float _bumpForce;

        [SerializeField] private float _hoverHeight;
        [SerializeField] private float _springStrength;
        [SerializeField] private float _springDamper;

        private PredictionRigidbody _predictionRigidbody;
        private Vector3 _currentLinearVelocity;
        private Quaternion _parentRotation;
        private Quaternion _sidecarLocalRotation;

        private IDrivingState _currentDrivingState;
        private IDrivingState _previousDrivingState;
        private DrivingStateTpye _currentStateType;
        private DrivingStateTpye _previousStateType;

        private IDrivingState _idleState = new IdleDrivingState();
        private IDrivingState _normalState = new NormalDrivingState();
        private IDrivingState _driftingState = new DriftingDrivingState();
        private IDrivingState _boostState = new BoostDrivingState();
        private IDrivingState _grassState = new GrassDrivingState();
        private IDrivingState _oilState = new OilDrivingState();

        private float _currentBatteryCharge;
        private float _batteryChargeTimer;
        private float _boostTimer;
        private float _driftDirection;
        private float _currentSteerInput;
        private bool _isOilAnimationActive;
        private float _oilAnimationTimer;

        private void Awake()
        {
            _predictionRigidbody = ObjectCaches<PredictionRigidbody>.Retrieve();
            _predictionRigidbody.Initialize(_movement);
            _parentRotation = _parent.rotation;
            _sidecarLocalRotation = _sidecar.localRotation;
            _currentDrivingState = _idleState;
            _currentStateType = DrivingStateTpye.Idle;
            _previousDrivingState = _idleState;
            _previousStateType = DrivingStateTpye.Idle;
            _currentLinearVelocity = Vector3.zero;
        }

        private void OnDestroy() => ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref _predictionRigidbody);

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += TimeManager_OnTick;
            TimeManager.OnPostTick += TimeManager_OnPostTick;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= TimeManager_OnTick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }

        private void TimeManager_OnTick() => RunInputs(CreateReplicateData());

        private ReplicateData CreateReplicateData()
        {
            if (!IsOwner)
                return default;
            return new ReplicateData(_input.SteerInput, _input.IsDrifting, _input.IsBoosting, _input.IsStarting);
        }

        [Replicate]
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            _currentDrivingState?.CheckStateChange(this, data);
            _currentDrivingState?.RunInputs(this, data);

            Vector3 repulsionForce = Vector3.zero;
            Collider[] hitColliders = Physics.OverlapSphere(_predictionRigidbody.Rigidbody.position, _bumpRadius, _sidecarLayerMask);
            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.transform.root == _parent.root) continue;

                Vector3 rawPushDirection = _predictionRigidbody.Rigidbody.position - hitCollider.ClosestPoint(_predictionRigidbody.Rigidbody.position);
                rawPushDirection.y = 0;
                float distance = rawPushDirection.magnitude;
                if (distance > 0 && distance < _bumpRadius)
                {
                    Vector3 forwardDir = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
                    forwardDir.y = 0;

                    var lateralPushDirection = Vector3.ProjectOnPlane(rawPushDirection, forwardDir.normalized);
                    //Debug.Log($"Lateral push direction: {lateralPushDirection}");

                    float pushStrength = 1f - (distance / _bumpRadius);

                    if (lateralPushDirection.sqrMagnitude > 0.001f)
                    {
                        Vector3 finalPush = lateralPushDirection.normalized * (_bumpForce * pushStrength);
                        repulsionForce += finalPush;

                        Debug.DrawRay(_predictionRigidbody.Rigidbody.position, forwardDir.normalized * 3f, Color.blue, 0.1f);
                        Debug.DrawRay(_predictionRigidbody.Rigidbody.position, rawPushDirection, Color.white, 0.1f);
                        Debug.DrawRay(_predictionRigidbody.Rigidbody.position, finalPush * 0.5f, Color.red, 0.5f);
                        //Debug.Log($"RawPushDirection: {rawPushDirection} | lateral {lateralPushDirection} | force {repulsionForce}");
                    }

                    MovementController otherDriver = hitCollider.GetComponentInParent<MovementController>();
                    if (otherDriver != null && !otherDriver.IsBoosting()) // TODO qui puoi mettere che se hai lo scudo non ricevi la collisione aumentata
                    {
                        repulsionForce *= (float)TimeManager.TickDelta;
                        Debug.Log("Trovato l'altro driver controller and is boosting");

                    }
                }
            }
            _currentLinearVelocity += repulsionForce;

            Vector3 velocityDifference = _currentLinearVelocity - _predictionRigidbody.Rigidbody.linearVelocity;
            _predictionRigidbody.AddForce(velocityDifference, ForceMode.VelocityChange);
            _predictionRigidbody.Simulate();

            _movement.transform.up = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;

            if (state != ReplicateState.Replayed)
                _currentSteerInput = data.SteerInput;
        }

        private void TimeManager_OnPostTick() => CreateReconcile();

        public override void CreateReconcile()
        {
            var rd = new ReconcileData(_predictionRigidbody, _parentRotation.eulerAngles.y, _sidecarLocalRotation.eulerAngles.y, _currentLinearVelocity, _driftDirection,
                                        _currentBatteryCharge, _batteryChargeTimer, _boostTimer, _oilAnimationTimer, _currentStateType, _previousStateType);
            ReconcileState(rd);
        }

        [Reconcile]
        private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            _parentRotation = Quaternion.Euler(0, data.ParentRotationY, 0);
            _sidecarLocalRotation = Quaternion.Euler(0, data.SidecarLocalRotationY, 0);

            //_currentLinearVelocity = data.CurrentLinearVelocity;
            _driftDirection = data.DriftDirection;
            _currentBatteryCharge = data.CurrentBatteryCharge;
            _batteryChargeTimer = data.BatteryChargeTimer;
            _boostTimer = data.BoostTimer;
            //_oilAnimationTimer = data.OilAnimationTimer;

            _currentStateType = data.StateType;
            _currentDrivingState = GetStateType(data.StateType);

            _previousStateType = data.PreviousStateType;
            _previousDrivingState = GetStateType(data.PreviousStateType);

            _predictionRigidbody.Reconcile(data.PredictionRigidbody);
        }

        Vector3 IDrivingStateContext.ParentForward => _parentRotation * Vector3.forward;
        Vector3 IDrivingStateContext.SidecarForward => (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
        SOSidecarStats IDrivingStateContext.NormalStats => _stats.NormalStats;

        SOSidecarStats IDrivingStateContext.BoostStats => _stats.BoostStats;

        SOSidecarStats IDrivingStateContext.GrassStats => _stats.GrassStats;

        SOBatteryStats IDrivingStateContext.BatteryStats => _stats.BatteryStats;
        SOSidecarAnimationStats IDrivingStateContext.AnimationStats => _stats.AnimationStats;
        IDrivingState IDrivingStateContext.PreviousDrivingState => _previousDrivingState;

        IDrivingState IDrivingStateContext.IdleState => _idleState;

        IDrivingState IDrivingStateContext.NormalState => _normalState;

        IDrivingState IDrivingStateContext.DriftingState => _driftingState;

        IDrivingState IDrivingStateContext.BoostState => _boostState;

        IDrivingState IDrivingStateContext.GrassState => _grassState;

        IDrivingState IDrivingStateContext.OilState => _oilState;

        float IDrivingStateContext.DriftDirection
        {
            get => _driftDirection;
            set => _driftDirection = value;
        }
        float IDrivingStateContext.CurrentBatteryCharge
        {
            get => _currentBatteryCharge;
            set => _currentBatteryCharge = value;
        }
        float IDrivingStateContext.BatteryChargeTimer
        {
            get => _batteryChargeTimer;
            set => _batteryChargeTimer = value;
        }
        float IDrivingStateContext.BoostTimer
        {
            get => _boostTimer;
            set => _boostTimer = value;
        }
        float IDrivingStateContext.OilAnimationTimer
        {
            get => _oilAnimationTimer;
            set => _oilAnimationTimer = value;
        }
        bool IDrivingStateContext.IsOilAnimationActive
        {
            get => _isOilAnimationActive;
            set => _isOilAnimationActive = value;
        }
        bool IDrivingStateContext.IsOnwer => IsOwner;
        bool IDrivingStateContext.IsServer => IsServerInitialized;

        void IDrivingStateContext.ApplyAcceleration(Vector3 direction, float accelerationForce, float maxSpeed)
        {
            Vector3 predictedVelocity = _predictionRigidbody.Rigidbody.linearVelocity + direction * (accelerationForce * (float)TimeManager.TickDelta);
            Vector3 horrizontalLinearVelocity = new(predictedVelocity.x, 0, predictedVelocity.z);
            if (horrizontalLinearVelocity.magnitude > maxSpeed)
            {
                Vector3 currentHorizontalVel = new(_predictionRigidbody.Rigidbody.linearVelocity.x, 0, _predictionRigidbody.Rigidbody.linearVelocity.z);
                if (currentHorizontalVel.magnitude > maxSpeed)
                {
                    float recoveryDrag = 15f;
                    Vector3 targetVel = currentHorizontalVel.normalized * maxSpeed;
                    var smoothedVel = Vector3.MoveTowards(currentHorizontalVel, targetVel, recoveryDrag * (float)TimeManager.TickDelta);
                    predictedVelocity = new Vector3(smoothedVel.x, predictedVelocity.y, smoothedVel.z);
                }
                else
                {
                    Vector3 limitedHorizontalVel = horrizontalLinearVelocity.normalized * maxSpeed;
                    predictedVelocity = new Vector3(limitedHorizontalVel.x, predictedVelocity.y, limitedHorizontalVel.z);
                }
            }
            _currentLinearVelocity = predictedVelocity;
        }
        void IDrivingStateContext.ApplyGravity(float gravity)
        {
            /*
            if (Physics.Raycast(_predictionRigidbody.Rigidbody.position, Vector3.down, out RaycastHit hit, _hoverHeight * 2f ))
            {
                float compression = _hoverHeight - hit.distance;
                if (compression > 0)
                {
                    float upwardAcceleration = (compression * _springStrength) - (_currentLinearVelocity.y * _springDamper);
                    _currentLinearVelocity += Vector3.up * upwardAcceleration * (float)TimeManager.TickDelta;
                }
            }
            */
            _currentLinearVelocity += Vector3.down * gravity * (float)TimeManager.TickDelta;
        }
        void IDrivingStateContext.ApplyLateralGrip(float lateralGripFactor)
        {
            Vector3 forwardDir = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
            Vector3 flatForwardDir = new Vector3(forwardDir.x, 0, forwardDir.z).normalized;

            Vector3 horizontalVel = new(_currentLinearVelocity.x, 0, _currentLinearVelocity.z);
            Vector3 targetHorizontalVel = flatForwardDir * horizontalVel.magnitude;
            var newHorizontalVel = Vector3.MoveTowards(horizontalVel, targetHorizontalVel, lateralGripFactor * (float)TimeManager.TickDelta);
            _currentLinearVelocity = new Vector3(newHorizontalVel.x, _currentLinearVelocity.y, newHorizontalVel.z);
        }
        void IDrivingStateContext.ApplySteering(float steerAmount, float steeringForce)
        {
            float steerAngle = steerAmount * steeringForce * (float)TimeManager.TickDelta;
            var steerRotation = Quaternion.AngleAxis(steerAngle, _parent.up);
            _parentRotation *= steerRotation;
        }
        void IDrivingStateContext.ApplyVisualRotation(Quaternion targetRot, float steerAngularRotationSlerp)
        {
            _sidecarLocalRotation = Quaternion.RotateTowards(
                _sidecarLocalRotation,
                targetRot,
                steerAngularRotationSlerp * (float)TimeManager.TickDelta
            );
        }
        void IDrivingStateContext.ChangeState(IDrivingState state, ReplicateData data)
        {
            _currentDrivingState?.Exit(this, data);

            _previousDrivingState = _currentDrivingState;
            _previousStateType = _currentStateType;

            _currentDrivingState = state;
            _currentStateType = GetStateType(state);
            _currentDrivingState?.Enter(this, data);
        }
        GroundType IDrivingStateContext.CheckGround()
        {
            if (Physics.Raycast(_movement.position, Vector3.down, out RaycastHit hit, 0.6f))
            {
                if (hit.collider.CompareTag("Grass"))
                {
                    Debug.Log("Colpito l'erba");
                    return GroundType.Grass;
                }
                else if (hit.collider.CompareTag("Oil"))
                {
                    Debug.Log("Passato su una chiazza di olio");
                    return GroundType.Oil;
                }
            }
            return GroundType.Normal;
        }
        float IDrivingStateContext.TickDelta() => (float)TimeManager.TickDelta;

        private IDrivingState GetStateType(DrivingStateTpye stateType)
        {
            switch (stateType)
            {
                case DrivingStateTpye.Idle: return _idleState;
                case DrivingStateTpye.Normal: return _normalState;
                case DrivingStateTpye.Drifting: return _driftingState;
                case DrivingStateTpye.Boost: return _boostState;
                case DrivingStateTpye.Grass: return _grassState;
                case DrivingStateTpye.Oil: return _oilState;
                default:
                    return _normalState;
            }
        }
        private DrivingStateTpye GetStateType(IDrivingState drivingState)
        {
            if (drivingState == _idleState) return DrivingStateTpye.Idle;
            else if (drivingState == _normalState) return DrivingStateTpye.Normal;
            else if (drivingState == _driftingState) return DrivingStateTpye.Drifting;
            else if (drivingState == _boostState) return DrivingStateTpye.Boost;
            else if (drivingState == _grassState) return DrivingStateTpye.Grass;
            else if (drivingState == _oilState) return DrivingStateTpye.Oil;
            else return DrivingStateTpye.Normal;
        }

        public Vector3 MovementPosition => _movement.transform.position;
        public Quaternion ParentRotation => _parentRotation;
        public Quaternion SidecarLocalRotation => _sidecarLocalRotation;


        public bool IsBoosting() => _currentStateType == DrivingStateTpye.Boost;
        public float CurrentSteerInput => _currentSteerInput;
    }
}
