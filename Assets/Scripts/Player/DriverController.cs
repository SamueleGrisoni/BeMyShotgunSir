using UnityEngine;

public class DriverController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _sphereMovementRb;
    [SerializeField]
    private Transform _visualModel;
    [SerializeField]
    private float _visualDriftMultiplier = 2.0f;
    [SerializeField]
    private float _accelerationForce = 10f;
    [SerializeField]
    private float _brakingForce = 10f;
    [SerializeField]
    private float _steeringSpeed = 90f;
    [SerializeField]
    private float _maxSpeedNormal = 25f; // _maxSpeedNormal is the max speed of the sidecar without boost;
    [SerializeField]
    private float _maxSpeedWithBoost = 35f;
    [SerializeField]
    private float _gripFactor = 0.9f;
    [SerializeField]
    private AnimationCurve _steeringCurve;
    private SphereCollider _sphereMovementCollider;
    // TODO temporanee1
    private Input_Actions _inputActions;
    private Input_Actions.GameplayActions _gameplayActions;
    private bool _isGrounded;
    private float _maxSpeed; // Actual max speed of the sidecar
    private float CurrentForwardSpeed => transform.InverseTransformDirection(_sphereMovementRb.linearVelocity).z; // Real speed of the Sidecar along the z axis (with rispect to the local reference system).
    private void Awake()
    {
        _sphereMovementCollider = _sphereMovementRb.GetComponent<SphereCollider>();
        // TODO cercare un modo per toglierlo
        _sphereMovementCollider.transform.parent = null; // Disjoint the sphere from the sidecar object. This avoid the SphereMovement to collide with colliders of the sidecar
        _inputActions = new Input_Actions();
        _inputActions.Enable();
        _gameplayActions = _inputActions.Gameplay;

        _maxSpeed = _maxSpeedNormal;
        _isGrounded = true;
    }
    private void Update() => ApplyVisualDrift();
    private void FixedUpdate()
    {
        transform.position = _sphereMovementRb.transform.position; // Since all the physics computation are done in FixedUpdate this line is put here to avoid Jittering
        //GroundNormalRotation();
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

            Vector3 currentVelocity = _sphereMovementRb.linearVelocity;
            var horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            Vector3 forwardDir = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 targetVelocity = forwardDir * horizontalVelocity.magnitude;

            // if _gripFactor = 1f -> the velocity of the sphere tends to targetVelocity, this means that the direction of the velocity of the sphere is instanlty equals to the direction of the sidecar. So the sidecar doesn't derapa.
            // if _gripFactor = 0.1f -> the velocity of the sphere tends to currentVelocity, this means that the direction of the velocity of the sphere slowly tends to be equals to the direction of the sidecar. So the sidecar derapa.
            // TODO magari il smoothness coefficient lo faccio diventare una variabile
            // 5f is the smoothness coefficient that determins how fast or slow the velocity of the sphere tends to targetVelocity
            var gripTargetVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, _gripFactor * Time.fixedDeltaTime * 5f);
            Vector3 gripCorrection = gripTargetVelocity - horizontalVelocity;
            _sphereMovementRb.AddForce(gripCorrection, ForceMode.VelocityChange);

            //if (Mathf.Abs(CurrentForwardSpeed) > _maxSpeed)
            //{
            //    _sphereMovementRb.linearVelocity = transform.forward * Mathf.Clamp(CurrentForwardSpeed, -_maxSpeed, _maxSpeed);
            //}
        }
    }
    private void OnDestroy()
    {
        // TODO Temporaneo
        _inputActions.Disable();
        _inputActions.Dispose();
    }

    private void Move(float accelInput, float breakInput)
    {
        if (accelInput > 0)
        {
            _sphereMovementRb.AddForce(transform.forward * (_accelerationForce * accelInput), ForceMode.Acceleration);
        }
        else if (breakInput > 0)
        {
            _sphereMovementRb.AddForce(-transform.forward * (_brakingForce * breakInput), ForceMode.Acceleration);
        }
        // TODO apply ground resistance when the sidecar is offroad?
    }

    private void Steer(float steerInput)
    {
        // Calculate speed-based steering modifier
        float speedRatio = Mathf.Abs(CurrentForwardSpeed) / _maxSpeed;
        float steeringMultiplier = _steeringCurve.Evaluate(speedRatio);

        float adjustedTurnSpeed = _steeringSpeed * steeringMultiplier;
        transform.Rotate(transform.up, steerInput * adjustedTurnSpeed * Time.fixedDeltaTime);
    }

    private void GroundNormalRotation()
    {
        //_isGrounded = Physics.SphereCast(_sphereMovementRb.position + transform.up * 0.1f, _sphereMovementCollider.radius, -transform.up, out RaycastHit hit);
        _isGrounded = Physics.Raycast(_sphereMovementRb.position, -transform.up, out RaycastHit hit, 1f);
        if (_isGrounded)
        {
            // Allignement of the sidecar to the ground
            transform.rotation = Quaternion.LookRotation(
                Vector3.Cross(transform.right, hit.normal),
                hit.normal);
        }
    }

    private void ApplyVisualDrift()
    {
        float lateralSpeed = -transform.InverseTransformDirection(_sphereMovementRb.linearVelocity).x;
        float driftAngle = lateralSpeed * _visualDriftMultiplier;
        _visualModel.localRotation = Quaternion.Euler(0, driftAngle, 0);
        //Quaternion targetVisualRotation = Quaternion.Euler(0, driftAngle, 0);
        //_visualModel.localRotation = Quaternion.Lerp(_visualModel.localRotation, targetVisualRotation, Time.deltaTime * 10f);
    }
}
