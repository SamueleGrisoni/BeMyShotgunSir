using System;
using BeMyShotgunSir.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
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
        Bump,
    }

    public enum CommitmentInfo
    {
        EnableNotUsed,
        EnableUsed,
        DisableOnSplit,
        Disable
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
        public float BumpTimer;
        public DrivingStateTpye StateType;
        public DrivingStateTpye PreviousStateType;
        public int GrassBuffer;
        public ReconcileData(PredictionRigidbody pr, float parentRotationY, float sidecarLocalRotationY, Vector3 currentLinearVelocity,
                                float driftDirection, float currentBatteryCharge, float batteryChargeTimer, float boostTimer, float oilAnimationTimer, float bumpTimer,
                                DrivingStateTpye stateType, DrivingStateTpye previousStateType, int grassBuffer) : this()
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
            BumpTimer = bumpTimer;
            StateType = stateType;
            PreviousStateType = previousStateType;
            GrassBuffer = grassBuffer;
        }
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    #endregion

    public class MovementController : NetworkBehaviour, IDrivingStateContext
    {
        #region Inspector

        [SerializeField] private DriverController _driverController;
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
        [SerializeField] private float _sidecarCollisionsRadius;
        [SerializeField] private float _sidecarCollisionsForce;

        [Header("Dead Reckoning")]
        [SerializeField] private float _steerDecaySeconds = 0.3f;

        [Header("Ground Snap")]
        [SerializeField] private float _hoverHeight = 0.256f;
        [SerializeField] private float _springForce = 50f;
        [SerializeField] private float _damping = 5f;
        [SerializeField] private float _customGravity = 20f;
        [SerializeField] private LayerMask _groundMask;

        [Header("Debug")]
        [SerializeField] private bool _log = false;
        [SerializeField] private bool _isDebugInputEnabled = false;
        [SerializeField] private bool _debugPowerUp = false; // TODO: rimuovere

        [Header("Grass")]
        [SerializeField] private int _onGrassBufferMin;

        #endregion

        #region Public
        public int? TeamId => _driverController == null ? null : _driverController.TeamId;
        public IDriverInputConsumer InputConsumer;
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

        #endregion

        #region  IDrivingStateContext state

        bool IDrivingStateContext.Log => _log;
        Vector3 IDrivingStateContext.ParentForward => _parentRotation * Vector3.forward;
        Vector3 IDrivingStateContext.SidecarForward => (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
        LayerMask IDrivingStateContext.CommitmentColliderLayer => _commitmentCollider.gameObject.layer;
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

        IDrivingState IDrivingStateContext.BumpState => _bumpState;

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
        float IDrivingStateContext.BumpTimer
        {
            get => _bumpTimer;
            set => _bumpTimer = value;
        }
        bool IDrivingStateContext.IsOnwer => IsOwner;
        bool IDrivingStateContext.IsServer => IsServerInitialized;
        CommitmentInfo IDrivingStateContext.CommitmentInfo => _commitmentInfo.Value;

        #endregion

        #region Private state
        private RaceNetContext _raceNetContext;

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
        private IDrivingState _bumpState = new BumpDrivingState();

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
        private float _bumpTimer;
        private ReplicateData _lastReplicateData;
        private float _activeDriftIntent = 0f;
        private float _predictedTicks;
        private int _onGrassBuffer = 0;
        private int _currentChunkId;
        private float _currentPossibleChargeEarlyCommitment;

        private readonly SyncVar<CommitmentInfo> _commitmentInfo = new(CommitmentInfo.Disable);

        public event Action<DrivingStateTpye> OnDrivingStateChanged;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _predictionRigidbody = ObjectCaches<PredictionRigidbody>.Retrieve();
            _predictionRigidbody.Initialize(_movement);

            _parentRotation = _parent.rotation;
            _sidecarLocalRotation = _sidecar.localRotation;
            _currentLinearVelocity = Vector3.zero;

            _currentDrivingState = _idleState;
            _currentStateType = DrivingStateTpye.Idle;
            _previousDrivingState = _idleState;
            _previousStateType = DrivingStateTpye.Idle;

            _isBoosting = false;
            _isOilAnimationActive = false;
            _commitmentDirection = CommitmentDirection.Default;
            _commitmentCollider.gameObject.layer = LayerMask.NameToLayer("DefaultWallCollider");
            _currentChunkId = 0;
            _currentPossibleChargeEarlyCommitment = 0;
        }

        private void OnDestroy() => ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref _predictionRigidbody);

        #endregion

        #region Initialization

        public void Initialize(RaceNetContext context, IDriverInputConsumer inputConsumer, DriverController driverController)
        {
            _driverController = driverController;
            _raceNetContext = context;
            InputConsumer = inputConsumer;
            if (InputConsumer != null)
            {
                InputConsumer.OnBoostPressed += ExecuteBoost;
                InputConsumer.OnEarlyCommitmentPressed += ExecuteInputEarlyCommitment;
                InputConsumer.OnDriverFeedbackPressed += ExecuteDriverFeedbackPressed;
            }
        }

        #endregion

        #region Fishnet network callbacks

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += TimeManager_OnTick;
            TimeManager.OnPostTick += TimeManager_OnPostTick;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= TimeManager_OnTick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
            if (InputConsumer != null)
            {
                InputConsumer.OnBoostPressed -= ExecuteBoost;
                InputConsumer.OnEarlyCommitmentPressed -= ExecuteInputEarlyCommitment;
                InputConsumer.OnDriverFeedbackPressed -= ExecuteDriverFeedbackPressed;
            }
        }

        #endregion

        #region Input events

        public void ExecuteBoost() => _isBoosting = true;
        public void ExecuteStart() => _isStarting = true;
        private void ExecuteInputEarlyCommitment(CommitmentDirection direction)
        {
            if (_commitmentInfo.Value == CommitmentInfo.EnableNotUsed)
            {
                _commitmentDirection = direction;
                _isBoosting = true;
            }

            ExecuteEarlyCommitment(direction);
        }
        public void ExecuteEarlyCommitmentDebug(CommitmentDirection direction)
        {
            if (_commitmentInfo.Value == CommitmentInfo.EnableNotUsed)
            {
                _commitmentDirection = direction;
                _isBoosting = true;
            }
            ExecuteEarlyCommitment(direction);
        }

        [ServerRpc]
        private void ExecuteDriverFeedbackPressed(DriverFeedback driverFeedback) => Server_DriverFeedback(driverFeedback);
        [Server]
        private void Server_DriverFeedback(DriverFeedback driverFeedback)
        {
            if (_raceNetContext.NetState.TryGetTeamData(TeamId.Value, out RaceTeamData teamData))
            {
                if (ServerManager.Clients.TryGetValue(teamData.ShotgunConnectionId, out NetworkConnection connShotgun))
                {
                    if (connShotgun != null)

                        Send_DriverFeedback(connShotgun, driverFeedback);
                }
            }
        }
        [TargetRpc]
        private void Send_DriverFeedback(NetworkConnection conn, DriverFeedback driverFeedback)
        {
            if (InputConsumer != null)
                InputConsumer.SendDriverFeedbackToShotgun(driverFeedback);
        }

        [Server]
        private void Server_BatteryEarlyCommitment(float batteryCharge)
        {
            if (_raceNetContext.NetState.TryGetTeamData(TeamId.Value, out RaceTeamData teamData))
            {
                if (ServerManager.Clients.TryGetValue(teamData.ShotgunConnectionId, out NetworkConnection connShotgun))
                {
                    if (connShotgun != null)
                        Send_BatteryEarlyCommitment(connShotgun, batteryCharge);
                }
                if (ServerManager.Clients.TryGetValue(teamData.DriverConnectionId, out NetworkConnection driverShotgun))
                {
                    if (driverShotgun != null)
                        Send_BatteryEarlyCommitment(driverShotgun, batteryCharge);
                }

            }
        }
        [TargetRpc]
        private void Send_BatteryEarlyCommitment(NetworkConnection conn, float batteryCharge)
        {
            if (InputConsumer != null)
                InputConsumer.BatteryChargeEarlyCommitment(batteryCharge);
        }

        [Server]
        private void Server_EarlyCommitmentExecuted(float batteryCharge, CommitmentDirection commitmentDirection)
        {
            if (_raceNetContext.NetState.TryGetTeamData(TeamId.Value, out RaceTeamData teamData))
            {
                if (ServerManager.Clients.TryGetValue(teamData.ShotgunConnectionId, out NetworkConnection connShotgun))
                {
                    if (connShotgun != null)
                        Send_EarlyCommitmentExecuted(connShotgun, batteryCharge, commitmentDirection);
                }
                if (ServerManager.Clients.TryGetValue(teamData.DriverConnectionId, out NetworkConnection driverShotgun))
                {
                    if (driverShotgun != null)
                        Send_EarlyCommitmentExecuted(driverShotgun, batteryCharge, commitmentDirection);
                }
            }
        }

        [TargetRpc]
        private void Send_EarlyCommitmentExecuted(NetworkConnection conn, float batteryCharge, CommitmentDirection commitmentDirection)
        {
            if (InputConsumer != null)
                InputConsumer.EarlyCommitmentExecuted(batteryCharge, commitmentDirection);
        }



        #endregion

        #region Tick loop

        private void TimeManager_OnTick() => RunInputs(CreateReplicateData());

        private ReplicateData CreateReplicateData()
        {
            if (!IsOwner)
                return default;

            ReplicateData rd = new();
            if (_isDebugInputEnabled)
            {
                UpdateActiveDriftIntent(_input.IsDrifting, _input.SteerInput);
                rd = new(_input.SteerInput, _input.IsDrifting, _activeDriftIntent, _isBoosting, _isStarting, _commitmentDirection);
                _isBoosting = false;
                _isStarting = false;
            }
            else
            {
                float steerInput = InputConsumer.IsDrifting ? InputConsumer.DriftInput : InputConsumer.SteerInput;
                UpdateActiveDriftIntent(InputConsumer.IsDrifting, steerInput);
                rd = new(steerInput, InputConsumer.IsDrifting, _activeDriftIntent, _isBoosting, InputConsumer.IsMoving, _commitmentDirection);
                _isBoosting = false;
                _isStarting = false;
            }
            return rd;
        }

        private void UpdateActiveDriftIntent(bool isDrifting, float steerInput)
        {
            if (!isDrifting)
                _activeDriftIntent = 0f;
            else if (_activeDriftIntent == 0f && Mathf.Abs(steerInput) > 0.1f)
                _activeDriftIntent = Mathf.Sign(steerInput);
        }

        [SerializeField] private bool _debugAim;
        [SerializeField] private bool _debugBumpState;
        [Replicate]
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (state.IsFuture() && !IsOwner)
            {
                _predictedTicks++;
                data = _lastReplicateData;

                if (_predictedTicks > 1)
                {
                    float steerDecayPerTick = (float)TimeManager.TickDelta / Mathf.Max(_steerDecaySeconds, 0.001f);
                    data.SteerInput = Mathf.MoveTowards(data.SteerInput, 0f, steerDecayPerTick);
                }

                _lastReplicateData = data;
                Log.DLazy(() => $"Predicted ticks {_predictedTicks}", this, _log);
            }
            else
            {
                _predictedTicks = 0;
                _lastReplicateData = data;
            }

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

            if (!_debugBumpState) // TODO togliere
                _commitmentCollider.gameObject.layer = GetCommitmentCollisionLayer(data.CommitmentDirection);

            _obstacleCollider.gameObject.layer = teamData.ActivePowerUpInfo.isArmorActive
                ? LayerMask.NameToLayer("ArmorLayer")
                : LayerMask.NameToLayer("ObstacleCollider");

            bool isReplayed = state.ContainsReplayed();
            _currentDrivingState?.CheckStateChange(this, data, isReplayed);
            _currentDrivingState?.RunInputs(this, data, isReplayed);

            ComputeCollisions(teamData, isReplayed);
            ApplyGroundSnap();

            _boxCollider.forward = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;

            Vector3 velocityDifference = _currentLinearVelocity - _predictionRigidbody.Rigidbody.linearVelocity;
            _predictionRigidbody.AddForce(velocityDifference, ForceMode.VelocityChange);
            _predictionRigidbody.Simulate();

            if (InputConsumer != null && TeamId.HasValue)
            {
                if (_raceNetContext.NetState.IsTeamMember(TeamId.Value))
                    InputConsumer.ChargeBattery = _currentBatteryCharge;
            }

            if (state != ReplicateState.Replayed)
                _currentSteerInput = data.SteerInput;

            _driverVisual.SetArmorVisualEffects(teamData.ActivePowerUpInfo.isArmorActive);
            _driverVisual.SetShieldVisualEffects(teamData.ActivePowerUpInfo.isShieldActive);
            _driverVisual.SetAimVisualEffects(teamData.ActivePowerUpInfo.isSpearPowerUpActive || teamData.ActivePowerUpInfo.isStealPowerUpActive);
        }

        private LayerMask GetCommitmentCollisionLayer(CommitmentDirection direction)
        {
            if (_commitmentInfo.Value == CommitmentInfo.EnableUsed)
            {
                return direction == CommitmentDirection.Left ? LayerMask.NameToLayer("RightCollider") : LayerMask.NameToLayer("LeftCollider");
            }
            else
                return LayerMask.NameToLayer("DefaultWallCollider");
        }

        private void ComputeCollisions(RaceTeamData teamData, bool isReplayed)
        {
            Vector3 repulsionForce = Vector3.zero;
            if (!teamData.ActivePowerUpInfo.isArmorActive)
            {
                Collider[] hitColliders = Physics.OverlapSphere(_predictionRigidbody.Rigidbody.position, _sidecarCollisionsRadius, _sidecarLayerMask);
                foreach (Collider hitCollider in hitColliders)
                {
                    if (hitCollider.transform.root == _parent.root) continue;

                    MovementController otherDriver = hitCollider.GetComponentInParent<MovementController>();
                    if (otherDriver != null)
                    {
                        CheckPowerUpCollisionEffect(teamData, otherDriver);
                    }
                    if (otherDriver != null && otherDriver.IsBoosting())
                    {
                        Vector3 rawPushDirection = _predictionRigidbody.Rigidbody.position - hitCollider.ClosestPoint(_predictionRigidbody.Rigidbody.position);
                        rawPushDirection.y = 0;
                        float distance = rawPushDirection.magnitude;
                        if (distance > 0 && distance < _sidecarCollisionsRadius)
                        {
                            Vector3 forwardDir = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
                            forwardDir.y = 0;

                            var lateralPushDirection = Vector3.ProjectOnPlane(rawPushDirection, forwardDir.normalized);

                            if (lateralPushDirection.sqrMagnitude > 0.001f)
                            {
                                Vector3 finalPush = lateralPushDirection.normalized * _sidecarCollisionsForce;
                                repulsionForce += finalPush;

                                Debug.DrawRay(_predictionRigidbody.Rigidbody.position, forwardDir.normalized * 3f, Color.blue, 0.1f);
                                Debug.DrawRay(_predictionRigidbody.Rigidbody.position, rawPushDirection, Color.white, 0.1f);
                                Debug.DrawRay(_predictionRigidbody.Rigidbody.position, finalPush * 0.5f, Color.red, 0.5f);

                                if (!isReplayed)
                                {
                                    Vector3 rightDir = (_parentRotation * _sidecarLocalRotation) * Vector3.right;
                                    rightDir.y = 0;
                                    float dotProduct = Vector3.Dot(rightDir.normalized, rawPushDirection.normalized);
                                    if (dotProduct < 0)
                                        _driverVisual.RightCollisionAnimation();
                                    else
                                        _driverVisual.LeftCollisionAnimation();
                                }
                            }
                        }
                    }
                }
            }
            _predictionRigidbody.AddForce(repulsionForce, ForceMode.Impulse);
        }

        private void CheckPowerUpCollisionEffect(RaceTeamData teamData, MovementController otherDriver)
        {
            if (IsServerInitialized)
            {
                if (teamData.ActivePowerUpInfo.isStealPowerUpActive)
                {
                    int? otherTeamId = otherDriver.GetTeamId();
                    Log.DLazy(() => $"Detection for steal power up active | TeamId {TeamId.Value} | Other TeamId {otherTeamId.Value}", this);
                    if (TeamId.HasValue && otherTeamId.HasValue)
                    {
                        int? powerUpIdentifier = _raceNetContext.PowerUpsNetController.GetInstanceId(TeamId.Value, PowerUp.StealPowerUp);
                        if (powerUpIdentifier.HasValue)
                        {
                            _raceNetContext.PowerUpsNetController.SetPowerUpTarget_ServerRpc(powerUpIdentifier.Value, otherTeamId.Value);
                            Log.DLazy(() => $"Use of PowerUp steal. Identifier {powerUpIdentifier.Value} | TeamId {TeamId.Value} | Other TeamId {otherTeamId.Value}", this);
                        }
                    }
                }
                if (teamData.ActivePowerUpInfo.isSpearPowerUpActive)
                {
                    int? otherTeamId = otherDriver.GetTeamId();
                    Log.DLazy(() => $"Detection for spear power up active | TeamId {TeamId.Value} | Other TeamId {otherTeamId.Value}", this);
                    if (TeamId.HasValue && otherTeamId.HasValue)
                    {
                        int? powerUpIdentifier = _raceNetContext.PowerUpsNetController.GetInstanceId(TeamId.Value, PowerUp.Spear);
                        if (powerUpIdentifier.HasValue)
                        {
                            _raceNetContext.PowerUpsNetController.SetPowerUpTarget_ServerRpc(powerUpIdentifier.Value, otherTeamId.Value);
                            Log.DLazy(() => $"Use of PowerUp spear. Identifier {powerUpIdentifier.Value} | TeamId {TeamId.Value} | Other TeamId {otherTeamId.Value}", this);
                        }
                    }
                }
            }
        }

        #endregion

        #region  Reconcile

        private void TimeManager_OnPostTick() => CreateReconcile();

        public override void CreateReconcile()
        {
            var rd = new ReconcileData(
                _predictionRigidbody,
                _parentRotation.eulerAngles.y,
                _sidecarLocalRotation.eulerAngles.y,
                _currentLinearVelocity,
                _driftDirection,
                _currentBatteryCharge,
                _batteryChargeTimer,
                _boostTimer,
                _oilAnimationTimer,
                _bumpTimer,
                _currentStateType,
                _previousStateType,
                _onGrassBuffer);
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
            _bumpTimer = data.BumpTimer;

            _currentStateType = data.StateType;
            _currentDrivingState = GetStateType(data.StateType);

            _previousStateType = data.PreviousStateType;
            _previousDrivingState = GetStateType(data.PreviousStateType);

            _onGrassBuffer = data.GrassBuffer;

            _predictionRigidbody.Reconcile(data.PredictionRigidbody);
        }

        #endregion

        #region IDrivingStateContext methods

        private void ChangeState(IDrivingState state, ReplicateData data, bool isReplayed)
        {
            _currentDrivingState?.Exit(this, data, isReplayed);
            _previousDrivingState = _currentDrivingState;
            _previousStateType = _currentStateType;
            _currentDrivingState = state;
            _currentStateType = GetStateType(state);
            _currentDrivingState?.Enter(this, data, isReplayed);

            if (!isReplayed)
            {
                OnDrivingStateChanged?.Invoke(_currentStateType);
            }
        }

        void IDrivingStateContext.ChangeState(IDrivingState state, ReplicateData data, bool isReplayed) => ChangeState(state, data, isReplayed);

        GroundType IDrivingStateContext.CheckGround()
        {
            if (Physics.SphereCast(_movement.position, 0.3f, Vector3.down, out RaycastHit hit, 0.6f))
            {
                if (hit.collider.CompareTag("Grass"))
                {
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

        void IDrivingStateContext.ApplyAcceleration(Vector3 direction, float accelerationForce, float maxSpeed)
        {
            float mass = _predictionRigidbody.Rigidbody.mass;
            float drag = _predictionRigidbody.Rigidbody.linearDamping;
            float dt = (float)TimeManager.TickDelta;

            Vector3 addedVelocity = (direction * accelerationForce / mass) * dt;
            float dragMultiplier = Mathf.Clamp01(1f - (drag * dt));

            _currentLinearVelocity = (_predictionRigidbody.Rigidbody.linearVelocity + addedVelocity) * dragMultiplier;
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
        void IDrivingStateContext.OilAnimation()
        {
            _driverVisual.OilAnimation();
        }


        bool IDrivingStateContext.CheckForkBarrierCollision()
        {
            LayerMask bumpLayer;
            if (_commitmentCollider.gameObject.layer == LayerMask.NameToLayer("RightCollider"))
                bumpLayer = LayerMask.GetMask("RightWallCollider");
            else if (_commitmentCollider.gameObject.layer == LayerMask.NameToLayer("LeftCollider"))
                bumpLayer = LayerMask.GetMask("LeftWallCollider");
            else
                return false;

            return Physics.CheckSphere(_predictionRigidbody.Rigidbody.position, _stats.AnimationStats.BumpRadius, bumpLayer);
        }

        void IDrivingStateContext.ApplyBump(float bumpForce, Vector3 direction) => _predictionRigidbody.AddForce(bumpForce * direction, ForceMode.Impulse);

        #endregion

        #region Ground snap
        private void ApplyGroundSnap() // TODO aggiungere check: se il sidecar si allontana troppo dal terreno (sia sopra che sotto) teletrasporta
        {
            float sphereRadious = 0.5f;

            if (Physics.SphereCast(transform.position, sphereRadious, Vector3.down, out RaycastHit hit, _hoverHeight + 1f, _groundMask))
            {
                float distance = hit.distance;
                float error = _hoverHeight - distance;

                float upwardVelocity = Vector3.Dot(_currentLinearVelocity, Vector3.up);
                float force = (error * _springForce) - (upwardVelocity * _damping);
                _predictionRigidbody.AddForce(Vector3.up * force, ForceMode.Acceleration);
            }
            else
            {
                _predictionRigidbody.AddForce(Vector3.down * _customGravity, ForceMode.Acceleration);
            }
        }

        #endregion

        #region Triggers and Collision

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Portal") && IsServerInitialized)
            {
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
                            if (lastSpecialChunk.Type == RoadChunkType.ENDING_CROSSROAD || lastSpecialChunk.Type == RoadChunkType.START_LINE)
                            // TODO controllare che il sidecar non torna e commita in chunk che ha già passato
                            {
                                float lenght = trackProgress.NextSpecialChunkId - (lastSpecialChunk.Id + 1);
                                float position = trackProgress.NextSpecialChunkId - _currentChunkId;
                                _currentPossibleChargeEarlyCommitment = (position / lenght) * 100;
                                Server_BatteryEarlyCommitment(_currentPossibleChargeEarlyCommitment);

                                if (_commitmentInfo.Value != CommitmentInfo.EnableUsed)
                                    _commitmentInfo.Value = CommitmentInfo.EnableNotUsed;
                            }
                            else if (lastSpecialChunk.Type == RoadChunkType.STARTING_CROSSROAD)
                            {
                                _commitmentInfo.Value = CommitmentInfo.DisableOnSplit;
                            }
                            else
                            {
                                _commitmentInfo.Value = CommitmentInfo.Disable;
                            }
                        }
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!TeamId.HasValue)
                return;

            if (IsOwner || (_raceNetContext.NetState.TryGetTeamData(TeamId.Value, out RaceTeamData teamData) && teamData.ShotgunConnectionId == base.LocalConnection.ClientId))
            {
                {
                    collision.gameObject.TryGetComponent(out ForkBarrier forkBarrier);
                    if (forkBarrier != null)
                    {
                        forkBarrier.SetBarrierVisible(true);
                    }
                }
            }
        }

        [ServerRpc]
        private void ExecuteEarlyCommitment(CommitmentDirection commitmentDirection)
        {
            if (_commitmentInfo.Value == CommitmentInfo.EnableNotUsed)
            {
                _currentBatteryCharge = Math.Min(_currentBatteryCharge + _currentPossibleChargeEarlyCommitment, 200);
                Server_EarlyCommitmentExecuted(_currentPossibleChargeEarlyCommitment, commitmentDirection);

                Log.DLazy(() => $"Commitmen executed on chunk {_currentChunkId} | Added: {_currentPossibleChargeEarlyCommitment} | Battery: {_currentBatteryCharge}", this);
                _commitmentInfo.Value = CommitmentInfo.EnableUsed;
            }
        }

        #endregion

        #region State mapping helpers
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
                case DrivingStateTpye.Bump: return _bumpState;
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
            else if (drivingState == _bumpState) return DrivingStateTpye.Bump;
            else return DrivingStateTpye.Normal;
        }

        public int? GetTeamId() => TeamId;

        public void StartRace()
        {
            if (IsOwner)
                _isStarting = true;
        }

        #endregion
    }
}
