using System;
using BeMyShotgunSir.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
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
        //public float DriftInput;
        public bool IsBoosting;
        public bool IsStarting;
        public CommitmentDirection CommitmentDirection;
        public ReplicateData(float steerInput, bool isDrifting, /*float driftInput,*/ bool isBoosting, bool isStarting, CommitmentDirection commitmentDirection) : this()
        {
            SteerInput = steerInput;
            IsDrifting = isDrifting;
            //DriftInput = driftInput;
            IsBoosting = isBoosting;
            IsStarting = isStarting;
            CommitmentDirection = commitmentDirection;
        }

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public PredictionRigidbody PredictionRigidbody;
        public float ParentRotationY;
        public float SidecarLocalRotationY;
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
        private bool _log = false;
        bool IDrivingStateContext.Log => _log;
        [SerializeField] private DriverController _driverController;
        public int? TeamId => _driverController == null ? null : _driverController.TeamId;
        public IDriverInputConsumer InputConsumer;
        private RaceNetContext _raceNetContext;
        public void Initialize(RaceNetContext context, IDriverInputConsumer inputConsumer)
        {
            //TODO set context references here
            _raceNetContext = context;
            InputConsumer = inputConsumer;
            InputConsumer.OnBoostPressed += ExecuteBoost;
            InputConsumer.OnEarlyCommitmentPressed += ExecuteEarlyCommitment;
        }
        public void ExecuteBoost() => _isBoosting = true;
        public void ExecuteEarlyCommitment(CommitmentDirection direction) => _commitmentDirection = direction;

        [SerializeField] private bool _isDebugInputEnabled = false;
        [SerializeField] private DriverInput _input;
        [SerializeField] private DriverStats _stats;
        [SerializeField] private DriverVisuals _driverVisual;

        [SerializeField] private Rigidbody _movement;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;
        [SerializeField] private Transform _sidecarModel;
        [SerializeField] private Transform _boxCollider;
        [SerializeField] private Collider _commitmentCollider;

        [SerializeField] private LayerMask _sidecarLayerMask;
        [SerializeField] private float _bumpRadius;
        [SerializeField] private float _bumpForce;

        [SerializeField] private float _decadimentoSteerInput = 0.05f;

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

        private bool _isBoosting;
        private CommitmentDirection _commitmentDirection;
        private float _currentBatteryCharge;
        private float _batteryChargeTimer;
        private float _boostTimer;
        private float _driftDirection;
        private float _currentSteerInput;
        private bool _isOilAnimationActive;
        private float _oilAnimationTimer;

        private int _currentChunkId;


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
            _isOilAnimationActive = false;
            _isBoosting = false;
            _commitmentDirection = CommitmentDirection.Default;
            _currentChunkId = 0;
        }

        private void OnDestroy() => ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref _predictionRigidbody);

        private void LateUpdate() => _boxCollider.forward = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += TimeManager_OnTick;
            TimeManager.OnPostTick += TimeManager_OnPostTick;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= TimeManager_OnTick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
            InputConsumer.OnBoostPressed -= ExecuteBoost;
            InputConsumer.OnEarlyCommitmentPressed -= ExecuteEarlyCommitment;
        }

        private void TimeManager_OnTick() => RunInputs(CreateReplicateData());

        private ReplicateData _lastReplicateData;
        private ReplicateData CreateReplicateData()
        {
            if (!IsOwner)
                return default;

            ReplicateData rd = new();
            if (_isDebugInputEnabled)
            {
                rd = new(_input.SteerInput, _input.IsDrifting, _input.IsBoosting, _input.IsStarting, _input.EarlyCommitment);
            }
            else
            {
                float steerInput = InputConsumer.IsDrifting ? InputConsumer.DriftInput : InputConsumer.SteerInput;
                rd = new(steerInput, InputConsumer.IsDrifting, _isBoosting, InputConsumer.IsMoving, _input.EarlyCommitment);
                _isBoosting = false;
            }
            return rd;
        }

        private float _predictedTicks;

        [Replicate]
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (state.IsFuture() && !IsOwner)
            {
                _predictedTicks++;
                data = _lastReplicateData;

                if (_predictedTicks > 1)
                {
                    data.SteerInput = Mathf.MoveTowards(data.SteerInput, 0f, _decadimentoSteerInput);
                }

                _lastReplicateData = data;
                Log.DLazy(() => $"Predicted ticks {_predictedTicks}", this);
            }
            else
            {
                _predictedTicks = 0;
                _lastReplicateData = data;
            }
            _commitmentCollider.gameObject.layer = data.CommitmentDirection == CommitmentDirection.Left ?
                                                    LayerMask.NameToLayer("LeftCollider") :
                                                    LayerMask.NameToLayer("RightCollider");

            bool isReplayed = state.ContainsReplayed();
            _currentDrivingState?.CheckStateChange(this, data, isReplayed);
            _currentDrivingState?.RunInputs(this, data, isReplayed);

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

                    float pushStrength = 1f - (distance / _bumpRadius);

                    MovementController otherDriver = hitCollider.GetComponentInParent<MovementController>();
                    if (lateralPushDirection.sqrMagnitude > 0.001f && otherDriver != null && otherDriver.IsBoosting())
                    {
                        Vector3 finalPush = lateralPushDirection.normalized * (_bumpForce * pushStrength);
                        repulsionForce += finalPush;

                        Debug.DrawRay(_predictionRigidbody.Rigidbody.position, forwardDir.normalized * 3f, Color.blue, 0.1f);
                        Debug.DrawRay(_predictionRigidbody.Rigidbody.position, rawPushDirection, Color.white, 0.1f);
                        Debug.DrawRay(_predictionRigidbody.Rigidbody.position, finalPush * 0.5f, Color.red, 0.5f);
                        //Debug.Log($"RawPushDirection: {rawPushDirection} | lateral {lateralPushDirection} | force {repulsionForce}");
                    }
                }
            }
            _currentLinearVelocity += repulsionForce;

            Vector3 velocityDifference = _currentLinearVelocity - _predictionRigidbody.Rigidbody.linearVelocity;
            _predictionRigidbody.AddForce(velocityDifference, ForceMode.VelocityChange);
            _predictionRigidbody.Simulate();

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
                    float recoveryDrag = 0.1f;
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
        void IDrivingStateContext.ApplyGravity(float gravity) => _currentLinearVelocity += Vector3.down * gravity * (float)TimeManager.TickDelta;
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
        void IDrivingStateContext.OilAnimation()
        {
            if (_isOilAnimationActive)
            {
                SendOilAnimation();
                _isOilAnimationActive = false;
            }
        }

        void IDrivingStateContext.ChangeState(IDrivingState state, ReplicateData data, bool isReplayed)
        {
            _currentDrivingState?.Exit(this, data, isReplayed);

            _previousDrivingState = _currentDrivingState;
            _previousStateType = _currentStateType;

            _currentDrivingState = state;
            _currentStateType = GetStateType(state);
            _currentDrivingState?.Enter(this, data, isReplayed);
        }
        GroundType IDrivingStateContext.CheckGround()
        {
            //return GroundType.Normal;
            if (Physics.SphereCast(_movement.position, 0.3f, Vector3.down, out RaycastHit hit, 0.6f))
            {
                if (hit.collider.CompareTag("Grass"))
                {
                    Log.DLazy(() => "Colpito l'erba", this, _log);
                    return GroundType.Grass;
                }
                else if (hit.collider.CompareTag("Oil"))
                {
                    Log.DLazy(() => "Passato su una chiazza di olio", this, _log);
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
        public bool IsOilAnimationActive
        {
            get => _isOilAnimationActive;
            set => _isOilAnimationActive = value;
        }

        public bool IsBoosting() => _currentStateType == DrivingStateTpye.Boost;
        public bool IsDrifting() => _currentStateType == DrivingStateTpye.Drifting;
        public float CurrentSteerInput => _currentSteerInput;
        public float CurrentOilAnimationTimer => _oilAnimationTimer;

        [ObserversRpc]
        private void SendOilAnimation() => _driverVisual.OilAnimation();

        [Server]
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Portal"))
            {
                RoadChunk roadChunk = other.transform.parent.GetComponent<RoadChunk>();
                if (roadChunk != null)
                {
                    _currentChunkId = roadChunk.ChunkNumber;
                    if (TeamId.HasValue)
                    {
                        _raceNetContext.NetState.TryGetTeamTrackProgress(TeamId.Value, out TeamTrackProgress trackProgress);
                        trackProgress.CurrentChunkId = _currentChunkId;
                        _raceNetContext.NetState.SetTeamTrackProgress(TeamId.Value, trackProgress);
                    }
                }
            }
        }
    }
}
