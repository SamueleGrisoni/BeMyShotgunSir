using System;
using System.Linq;
using BeMyShotgunSir.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
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
        public float DriftIntent;
        public bool IsBoosting;
        public bool IsStarting;
        public CommitmentDirection CommitmentDirection;
        public ReplicateData(float steerInput, bool isDrifting, float driftIntent, bool isBoosting, bool isStarting, CommitmentDirection commitmentDirection) : this()
        {
            SteerInput = steerInput;
            IsDrifting = isDrifting;
            DriftIntent = driftIntent;
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
            InputConsumer.OnEarlyCommitmentPressed += ExecuteInputEarlyCommitment;

        }
        public void ExecuteBoost() => _isBoosting = true;
        public void ExecuteInputEarlyCommitment(CommitmentDirection direction)
        {
            _commitmentDirection = direction;
            ExecuteEarlyCommitment();
        }

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
        [SerializeField] private Collider _obstacleCollider;

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
        private bool _isStarting;
        private CommitmentDirection _commitmentDirection;
        private float _currentBatteryCharge;
        private float _batteryChargeTimer;
        private float _boostTimer;
        private float _driftDirection;
        private float _currentSteerInput;
        private bool _isOilAnimationActive;
        private float _oilAnimationTimer;
        private ReplicateData _lastReplicateData;


        private int _currentChunkId;
        private float _currentPossibleChargeEarlyCommitment;
        //private bool _earlyCommitmentEnabled;

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
            _earlyCommitmentEnabled = false;
            _earlyCommitmentNotUsed = true;
            _currentPossibleChargeEarlyCommitment = 0;
        }

        private void OnDestroy() => ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref _predictionRigidbody);

        private void LateUpdate()
        {
            Debug.Log($"Current chunkid: {_currentChunkId} | Current battery: {_currentBatteryCharge} | Early Commitment is enabled {_earlyCommitmentEnabled}");
        }

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
            InputConsumer.OnEarlyCommitmentPressed -= ExecuteInputEarlyCommitment;
        }

        private void TimeManager_OnTick() => RunInputs(CreateReplicateData());

        private float _activeDriftIntent = 0f;
        private ReplicateData CreateReplicateData()
        {
            if (!IsOwner)
                return default;

            ReplicateData rd = new();
            if (_isDebugInputEnabled)
            {
                if (!_input.IsDrifting)
                {
                    _activeDriftIntent = 0f;
                }
                else if (_activeDriftIntent == 0f && Mathf.Abs(_input.SteerInput) > 0.1f)
                {
                    _activeDriftIntent = Mathf.Sign(_input.SteerInput);
                }
                rd = new(_input.SteerInput, _input.IsDrifting, _activeDriftIntent, _input.IsBoosting, _input.IsStarting, _commitmentDirection);
                _input.IsStarting = false;
            }
            else
            {
                float steerInput = InputConsumer.IsDrifting ? InputConsumer.DriftInput : InputConsumer.SteerInput;

                if (!InputConsumer.IsDrifting)
                {
                    _activeDriftIntent = 0f;
                }
                else if (_activeDriftIntent == 0f && Mathf.Abs(steerInput) > 0.1f)
                {
                    _activeDriftIntent = Mathf.Sign(InputConsumer.SteerInput);
                }
                rd = new(steerInput, InputConsumer.IsDrifting, _activeDriftIntent, _isBoosting, InputConsumer.IsMoving, _commitmentDirection);
                _isBoosting = false;
            }
            return rd;
        }

        private float _predictedTicks;
        [SerializeField] private bool _debugPowerUp = true; // TODO temporaneo

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

            RaceTeamData teamData = default;
            if (_debugPowerUp)
            {
                teamData.ActivePowerUpInfo.isArmorActive = _input.IsArmorActive;
                teamData.ActivePowerUpInfo.isStealPowerUpActive = _input.IsStealActive;
            }
            else if (TeamId.HasValue)
            {
                _raceNetContext.NetState.TryGetTeamData(TeamId.Value, out teamData);
            }

            _obstacleCollider.gameObject.layer = teamData.ActivePowerUpInfo.isArmorActive ?
                                                        LayerMask.NameToLayer("ArmorLayer") :
                                                        LayerMask.NameToLayer("ObstacleCollider");

            bool isReplayed = state.ContainsReplayed();
            _currentDrivingState?.CheckStateChange(this, data, isReplayed);
            _currentDrivingState?.RunInputs(this, data, isReplayed);

            Vector3 repulsionForce = Vector3.zero;
            if (!teamData.ActivePowerUpInfo.isArmorActive)
            {
                Collider[] hitColliders = Physics.OverlapSphere(_predictionRigidbody.Rigidbody.position, _bumpRadius, _sidecarLayerMask);
                foreach (Collider hitCollider in hitColliders)
                {
                    if (hitCollider.transform.root == _parent.root) continue;

                    if (teamData.ActivePowerUpInfo.isStealPowerUpActive)
                    {
                        Log.DLazy(() => $"Detection for steal power up active", this);
                    }

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
            }
            _currentLinearVelocity += repulsionForce;
            ApplyGroundSnap();

            _boxCollider.forward = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;

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

            _currentLinearVelocity = data.CurrentLinearVelocity;
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
        [SerializeField] private float _hoverHeight = 0.256f;
        [SerializeField] private float _springForce = 50f;
        [SerializeField] private float _damping = 5f;
        [SerializeField] private float _customGravity = 20f;
        [SerializeField] private LayerMask _groundMask;

        private void ApplyGroundSnap() // TODO aggiungere check: se il sidecar si allontana troppo dal terreno (sia sopra che sotto) teletrasporta
        {
            float sphereRadious = 0.5f;

            if (Physics.SphereCast(transform.position, sphereRadious, Vector3.down, out RaycastHit hit, _hoverHeight + 1f, _groundMask))
            {
                float distance = hit.distance;
                float error = _hoverHeight - distance;

                float upwardVelocity = Vector3.Dot(_currentLinearVelocity, Vector3.up);
                float force = (error * _springForce) - (upwardVelocity * _damping);
                //_currentLinearVelocity += Vector3.up * force * (float)TimeManager.TickDelta;
                _predictionRigidbody.AddForce(Vector3.up * force, ForceMode.Acceleration);

            }
            else
            {
                //_currentLinearVelocity += Vector3.down * _customGravity * (float)TimeManager.TickDelta;
                _predictionRigidbody.AddForce(Vector3.down * _customGravity, ForceMode.Acceleration);
            }
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

        private int _onGrassBuffer = 0;
        [SerializeField] private int _onGrassBufferMin;
        GroundType IDrivingStateContext.CheckGround()
        {
            if (Physics.SphereCast(_movement.position, 0.3f, Vector3.down, out RaycastHit hit, 0.6f))
            {
                if (hit.collider.CompareTag("Grass"))
                {
                    //Debug.Log("Hit grass");
                    _onGrassBuffer++;
                    if (_onGrassBuffer > _onGrassBufferMin) return GroundType.Grass;
                }
                else
                {
                    _onGrassBuffer = 0;
                }

                if (hit.collider.CompareTag("Oil"))
                {
                    Log.DLazy(() => "Passato su una chiazza di olio", this);
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

        private bool _earlyCommitmentNotUsed;
        private bool _earlyCommitmentEnabled;
        [Server]
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Portal"))
            {
                Debug.Log("hit portale");
                RoadChunk roadChunk = other.transform.parent.GetComponent<RoadChunk>();
                if (roadChunk != null)
                {
                    _currentChunkId = roadChunk.ChunkNumber;
                    if (TeamId.HasValue)
                    {
                        if (_raceNetContext.NetState.TryGetTeamTrackProgress(TeamId.Value, out TeamTrackProgress trackProgress))
                        {
                            trackProgress.CurrentChunkId = _currentChunkId;
                            _raceNetContext.NetState.SetTeamTrackProgress(TeamId.Value, trackProgress);

                            PortalInfo lastSpecialChunk = trackProgress.LastSpecialChunkType.Value;
                            if (lastSpecialChunk.Type == RoadChunkType.ENDING_CROSSROAD || lastSpecialChunk.Type == RoadChunkType.START_LINE) // TODO per ora viene ignorato il primo rettilineo
                            {
                                // I am in the straight
                                float lenght = trackProgress.NextSpecialChunkId - (lastSpecialChunk.Id + 1);
                                float position = trackProgress.NextSpecialChunkId - _currentChunkId;
                                _currentPossibleChargeEarlyCommitment = (position / lenght) * 100;
                                Debug.Log($"Current {_currentChunkId} | Last {lastSpecialChunk.Id} | Next {trackProgress.NextSpecialChunkId} | Charge {_currentPossibleChargeEarlyCommitment}");
                                _earlyCommitmentEnabled = true;
                            }
                            else
                            {
                                _earlyCommitmentEnabled = false;
                                _earlyCommitmentNotUsed = true;
                            }
                        }
                    }
                }
            }
        }
        [ServerRpc]
        public void ExecuteEarlyCommitment()
        {
            if (_earlyCommitmentNotUsed && _earlyCommitmentEnabled)
            {
                _currentBatteryCharge = Math.Min(_currentBatteryCharge + _currentPossibleChargeEarlyCommitment, 200);
                Log.DLazy(() => $"Commitmen executed on chunk {_currentChunkId} | Added: {_currentPossibleChargeEarlyCommitment} | Battery: {_currentBatteryCharge}", this);
                _earlyCommitmentNotUsed = false;
            }
        }

        // TODO temporaneo
        public void ExecuteEarlyCommitmentDebug(CommitmentDirection commitmentDirection)
        {
            _commitmentDirection = commitmentDirection;
            ExecuteEarlyCommitment();
        }

    }
}
