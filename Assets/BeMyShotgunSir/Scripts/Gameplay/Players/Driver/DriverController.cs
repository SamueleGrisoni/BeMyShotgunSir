using BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    /*  Questo enum serve per identificare e riconciliare gli stati tra client e server.
        Ci sono dei casi in cui sul client e sul server il sidecar non si trova nello stesso stato e quindi sul client
        deve essere riconciliato usando il replicate data.
    */
    public enum DrivingStateType : byte
    {
        Idle,
        Normal,
        Drifting,
        Boost,
    }

    /*  This struct contains the inputs of the owner client required to calculate the next state of the
        sidecar. Each ReplicateData is associated with a specific 'Tick' allowing FishNet to keep track
        of exactly when the input state occured.
        It is used on the owner clients to implement the CSP: the client immediately exectues the logic
        giving the player the felling of zero latency.
        It is also sent to the Authoritative Server and other Clients (since it is enabled StateForwarding).
    */
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
    /*  ReconcileData is the actual snapshot of the sidecar sent by the Server to the Clients.
        When the clients receives a ReconcileData, it compares its prediction with the server's data.
        If the difference exceeds a certain threshold the client uses ReconcileData to reconcile the client.
    */
    public struct ReconcileData : IReconcileData
    {
        public PredictionRigidbody PredictionRigidbody;
        public Quaternion ParentRotation;
        public Quaternion SidecarLocalRotation;
        public Vector3 CurrentLinearVelocity;
        public float DriftDirection;
        public float CurrentBatteryCharge;
        public float BatteryChargeTimer;
        public float BoostTimer;
        public DrivingStateType StateType;
        public ReconcileData(PredictionRigidbody pr, Quaternion parentRotation, Quaternion sidecarLocalRotation, Vector3 currentLinearVelocity, float driftDirection, float currentBatteryCharge, float batteryChargeTimer, float boostTimer, DrivingStateType stateType) : this()
        {
            PredictionRigidbody = pr;
            ParentRotation = parentRotation;
            SidecarLocalRotation = sidecarLocalRotation;
            CurrentLinearVelocity = currentLinearVelocity;
            DriftDirection = driftDirection;
            CurrentBatteryCharge = currentBatteryCharge;
            BatteryChargeTimer = batteryChargeTimer;
            BoostTimer = boostTimer;
            StateType = stateType;
        }
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
    public class DriverController : NetworkBehaviour, IDriverControllerContext
    {
        [SerializeField] private SOSidecarStats _stats;
        [SerializeField] private Rigidbody _sphere;
        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _sidecar;

        private PredictionRigidbody _predictionRigidbody;
        private Vector3 _currentLinearVelocity;
        private Quaternion _parentRotation;
        private Quaternion _sidecarLocalRotation;
        private IDrivingState _currentDrivingState;
        private DrivingStateType _currentStateType;
        private IDrivingState _idleState = new IdleDrivingState();
        private IDrivingState _normalState = new NormalDrivingState();
        private IDrivingState _driftingState = new DriftingDrivingState();
        private IDrivingState _boostState = new BoostDrivingState();
        private float _currentMaxSpeed;
        private float _steerInput;
        private bool _isDrifting;
        private bool _isBoosting;
        private float _currentBatteryCharge;
        private float _batteryChargeTimer;
        private float _boostTimer;
        private float _driftDirection;
        private bool _isStarting;

        Vector3 IDriverControllerContext.ParentForward => _parentRotation * Vector3.forward;
        Vector3 IDriverControllerContext.SidecarForward => (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
        SOSidecarStats IDriverControllerContext.Stats => _stats;
        IDrivingState IDriverControllerContext.IdleState => _idleState;
        IDrivingState IDriverControllerContext.NormalState => _normalState;
        IDrivingState IDriverControllerContext.DriftingState => _driftingState;
        IDrivingState IDriverControllerContext.BoostState => _boostState;
        float IDriverControllerContext.CurrentMaxSpeed => _currentMaxSpeed;
        bool IDriverControllerContext.IsGrounded => throw new System.NotImplementedException();
        bool IDriverControllerContext.IsDriftingButtonPressed => throw new System.NotImplementedException();
        float IDriverControllerContext.SteerInput => throw new System.NotImplementedException();
        bool IDriverControllerContext.IsBoostButtonPressed => throw new System.NotImplementedException();
        bool IDriverControllerContext.IsStartButtonPressed => throw new System.NotImplementedException();
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
        float IDriverControllerContext.BatteryChargeTimer
        {
            get => _batteryChargeTimer;
            set => _batteryChargeTimer = value;
        }
        float IDriverControllerContext.BoostTimer
        {
            get => _boostTimer;
            set => _boostTimer = value;
        }

        public IDrivingState AirState => throw new System.NotImplementedException();

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
            {
                GetComponent<PlayerInput>().enabled = true;

            }
        }
        private void Awake()
        {
            _predictionRigidbody = ObjectCaches<PredictionRigidbody>.Retrieve();
            _predictionRigidbody.Initialize(_sphere);
            _parentRotation = _parent.rotation;
            _sidecarLocalRotation = _sidecar.localRotation;
            _currentMaxSpeed = _stats.MaxSpeed;
            _currentDrivingState = _idleState;
            _currentStateType = DrivingStateType.Idle;
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

            var md = new ReplicateData(_steerInput, _isDrifting, _isBoosting, _isStarting);
            return md;
        }
        [Replicate]
        /*
        Questo metodo viene eseguito sia sul Client che sul Server per ridurre i problemi di latenza.
        Il client esegue immediatamente il codice, senza aspettare il server
        Il server esegue anch'esso i calcoli una volta ricevuti i dati (ReplicateDate)
        */
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            _currentDrivingState?.CheckStateChange(this, data);
            _currentDrivingState?.RunInputs(this, data);

            _predictionRigidbody.Velocity(_currentLinearVelocity);
            _predictionRigidbody.Simulate();
        }
        private void TimeManager_OnPostTick() => CreateReconcile();
        public override void CreateReconcile()
        {
            if (!IsServerInitialized)
                return;

            var rd = new ReconcileData(_predictionRigidbody, _parentRotation, _sidecarLocalRotation, _currentLinearVelocity, _driftDirection, _currentBatteryCharge, _batteryChargeTimer, _boostTimer, _currentStateType);
            ReconcileState(rd, Channel.Reliable);
        }
        [Reconcile]
        /*
        Dopo ogni tick, il server invia al client un pacchetto ReconcileData che contiene lo stato ufficiale.
        Il client riceve il pacchetto ReconcileData, lo confronta con la sua predizione e se c'è una differenza
        significativa il client sovrascrive i propri dati con quelli del server.
        */
        private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            _parentRotation = data.ParentRotation;
            _sidecarLocalRotation = data.SidecarLocalRotation;

            _currentLinearVelocity = data.CurrentLinearVelocity;
            _driftDirection = data.DriftDirection;
            _currentBatteryCharge = data.CurrentBatteryCharge;
            _batteryChargeTimer = data.BatteryChargeTimer;
            _boostTimer = data.BoostTimer;

            _currentStateType = data.StateType;
            switch (_currentStateType)
            {
                case DrivingStateType.Idle: _currentDrivingState = _idleState; break;
                case DrivingStateType.Normal: _currentDrivingState = _normalState; break;
                case DrivingStateType.Drifting: _currentDrivingState = _driftingState; break;
                case DrivingStateType.Boost: _currentDrivingState = _boostState; break;
                default:
                    break;
            }

            _predictionRigidbody.Reconcile(data.PredictionRigidbody);
        }

        public void LateUpdate()
        {
            // TODO aggiungere if (!base.IsReconciling) per bloccare la graphica quando si fa il resimulation
            // TODO aggiungere interpolazione per rendere la transizione tra due tick molto più smooth.
            _parent.position = _sphere.transform.position;
            //if (!IsOwner && !IsServerInitialized) return;
            _parent.rotation = _parentRotation;
            _sidecar.localRotation = _sidecarLocalRotation;
        }
        void IDriverControllerContext.ApplySteering(float steerAmount)
        {
            float steerAngle = steerAmount * _stats.SteeringForce * (float)TimeManager.TickDelta;
            var steerRotation = Quaternion.AngleAxis(steerAngle, _parent.up);
            _parentRotation *= steerRotation;
        }
        void IDriverControllerContext.ApplyAcceleration(Vector3 direction)
        {
            Vector3 predictedVelocity = _predictionRigidbody.Rigidbody.linearVelocity + direction * (_stats.AccelerationForce * (float)TimeManager.TickDelta);
            Vector3 horrizontalLinearVelocity = new(predictedVelocity.x, 0, predictedVelocity.z);
            if (horrizontalLinearVelocity.magnitude > _currentMaxSpeed)
            {
                Vector3 limitedHorizontalVel = horrizontalLinearVelocity.normalized * _currentMaxSpeed;
                predictedVelocity = new Vector3(limitedHorizontalVel.x, predictedVelocity.y, limitedHorizontalVel.z);
            }
            _currentLinearVelocity = predictedVelocity;
        }
        // TODO implementare gravità
        void IDriverControllerContext.ApplyGravity(float gravity) => _predictionRigidbody.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        void IDriverControllerContext.ApplyLateralGrip()
        {
            Vector3 forwardDir = (_parentRotation * _sidecarLocalRotation) * Vector3.forward;
            Vector3 flatForwardDir = new Vector3(forwardDir.x, 0, forwardDir.z).normalized;

            Vector3 horizontalVel = new(_currentLinearVelocity.x, 0, _currentLinearVelocity.z);
            Vector3 targetHorizontalVel = flatForwardDir * horizontalVel.magnitude;
            var newHorizontalVel = Vector3.MoveTowards(horizontalVel, targetHorizontalVel, _stats.LateralGripFactor * (float)TimeManager.TickDelta);
            _currentLinearVelocity = new Vector3(newHorizontalVel.x, _currentLinearVelocity.y, newHorizontalVel.z);
        }
        void IDriverControllerContext.ApplyVisualRotation(Quaternion targetRot)
        {
            _sidecarLocalRotation = Quaternion.RotateTowards(
                _sidecarLocalRotation,
                targetRot,
                _stats.SteerAngularRotationSlerp * (float)TimeManager.TickDelta
            );
        }
        void IDriverControllerContext.AnimateSidecar() => throw new System.NotImplementedException();
        void IDriverControllerContext.ChangeState(IDrivingState state, ReplicateData data)
        {
            _currentDrivingState?.Exit(this, data);
            _currentDrivingState = state;

            if (state == _idleState) _currentStateType = DrivingStateType.Idle;
            else if (state == _normalState) _currentStateType = DrivingStateType.Normal;
            else if (state == _driftingState) _currentStateType = DrivingStateType.Drifting;
            else if (state == _boostState) _currentStateType = DrivingStateType.Boost;
            _currentDrivingState?.Enter(this, data);
        }
        void IDriverControllerContext.SetMaxSpeed(float maxSpeed) => _currentMaxSpeed = maxSpeed;
        void IDriverControllerContext.SetDriftDirection(float driftDirection) => _driftDirection = driftDirection;
        float IDriverControllerContext.TickDelta() => (float)TimeManager.TickDelta;
        bool IDriverControllerContext.IsOnwer => IsOwner;
        bool IDriverControllerContext.IsServer => IsServerInitialized;

        // TODO aggiungere controlli sull'input (forse)
        private void OnSteer(InputValue value) => _steerInput = value.Get<float>();
        private void OnStart(InputValue value) => _isStarting = value.isPressed;
        private void OnDrift(InputValue value) => _isDrifting = value.isPressed;
        private void OnBoost(InputValue value) => _isBoosting = value.isPressed;

    }
}
