using UnityEngine;

public class DriverController : MonoBehaviour
{
    [SerializeField] private SOSidecarStats _stats;
    [SerializeField] private Rigidbody _movementBodyRb;
    [SerializeField] private Transform _sidecar;
    [SerializeField] private Transform _visualModel;
    private Input_Actions _inputActions;
    private Input_Actions.GameplayActions _gameplayActions;
    private bool _isGrounded;
    private float _maxSpeed; // Actual max speed of the sidecar
    private float CurrentForwardSpeed => transform.InverseTransformDirection(_movementBodyRb.linearVelocity).z; // Real speed of the Sidecar along the z axis (with rispect to the local reference system).
    private void Awake()
    {
        _inputActions = new Input_Actions();
        _inputActions.Enable();
        _gameplayActions = _inputActions.Gameplay;

        _maxSpeed = _stats.MaxSpeedDefault;
        _isGrounded = true;
    }
    private void Update() => ApplyVisualDrift();
    private void FixedUpdate()
    {
        transform.position = _movementBodyRb.transform.position; // Since all the physics computation are done in FixedUpdate this line is put here to avoid Jittering
        GroundNormalRotation();
        if (_isGrounded)
        {
            // Rotate sidecar
            float steerInput = _gameplayActions.TempSteer.ReadValue<float>();
            float accelInput = _gameplayActions.TempAccelerate.ReadValue<float>();
            float breakInput = _gameplayActions.TempBrake.ReadValue<float>();

            // Apply movement forces
            Move(accelInput, breakInput);

            // Apply steering forces
            Steer(steerInput);

            Vector3 currentVelocity = _movementBodyRb.linearVelocity;
            var horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            Vector3 forwardDir = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 targetVelocity = forwardDir * horizontalVelocity.magnitude;

            // if _gripFactor = 1f -> the velocity of the sphere tends to targetVelocity, this means that the direction of the velocity of the sphere is instanlty equals to the direction of the sidecar. So the sidecar doesn't derapa.
            // if _gripFactor = 0.1f -> the velocity of the sphere tends to currentVelocity, this means that the direction of the velocity of the sphere slowly tends to be equals to the direction of the sidecar. So the sidecar derapa.
            var gripTargetVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, _stats.GripFactor * Time.fixedDeltaTime);
            Vector3 gripCorrection = gripTargetVelocity - horizontalVelocity;
            _movementBodyRb.AddForce(gripCorrection, ForceMode.VelocityChange);
        }
    }
    private void OnDestroy()
    {
        _inputActions.Disable();
        _inputActions.Dispose();
    }

    private void Move(float accelInput, float breakInput)
    {
        if (accelInput > 0)
        {
            _movementBodyRb.AddForce(transform.forward * (_stats.AccelerationForce * accelInput), ForceMode.Acceleration);
        }
        else if (breakInput > 0)
        {
            _movementBodyRb.AddForce(-transform.forward * (_stats.BrakingForce * breakInput), ForceMode.Acceleration);
        }
    }

    private void Steer(float steerInput)
    {
        // Calculate speed-based steering modifier
        float speedRatio = Mathf.Abs(CurrentForwardSpeed) / _maxSpeed;
        float steeringMultiplier = _stats.SteeringCurve.Evaluate(speedRatio);

        float adjustedTurnSpeed = _stats.SteeringSpeed * steeringMultiplier;
        transform.Rotate(transform.up, steerInput * adjustedTurnSpeed * Time.fixedDeltaTime);
    }

    private void GroundNormalRotation()
    {
        _isGrounded = Physics.Raycast(_movementBodyRb.position, -transform.up, out RaycastHit hit, 1f);
        if (_isGrounded)
        {
            var targetRotation = Quaternion.LookRotation(
                Vector3.Cross(transform.right, hit.normal),
                hit.normal);
            _sidecar.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _stats.GroundAlignSpeed * Time.fixedDeltaTime);
        }
    }

    private void ApplyVisualDrift()
    {
        float lateralSpeed = -transform.InverseTransformDirection(_movementBodyRb.linearVelocity).x;
        float driftAngle = lateralSpeed * _stats.VisualDriftMultiplier;
        _visualModel.localRotation = Quaternion.Euler(0, driftAngle, 0);
        //Quaternion targetVisualRotation = Quaternion.Euler(0, driftAngle, 0);
        //_visualModel.localRotation = Quaternion.Lerp(_visualModel.localRotation, targetVisualRotation, Time.deltaTime * 10f);
    }
}
