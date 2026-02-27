using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
public class DriverController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _rigidBody;
    [SerializeField]
    private float _accelForce = 10f;
    [SerializeField]
    private float _brakeForce = 10f;
    [SerializeField]
    private float _turnSpeed = 90f;
    [SerializeField]
    private float _maxSpeed = 10f;
    // TODO da capire se è da tenere. Se la curva all fine rimane una retta costante a 1 allora si può anche togliere
    [SerializeField]
    private AnimationCurve _steerCurve;
    [SerializeField]
    private float _gripFactor = 0.9f;
    private SphereCollider _sphereCollider;
    private Input_Actions _inputActions;
    private Input_Actions.GameplayActions _gameplayActions;
    private bool _grounded;
    private float _currentSpeed;
    private void Awake()
    {
        _sphereCollider = _rigidBody.GetComponent<SphereCollider>();
        // Disjoint the sphere from the sidecar object
        _sphereCollider.transform.parent = null;
        _inputActions = new Input_Actions();
        _inputActions.Enable();
        _gameplayActions = _inputActions.Gameplay;

    }
    private void Update() => transform.position = _rigidBody.position;
    //private void OnValidate() => TryGetComponent(out _rigidBody);
    private void FixedUpdate()
    {
        _currentSpeed = _rigidBody.linearVelocity.magnitude;
        // TODO da capire meglio questa riga
        _grounded = Physics.SphereCast(_rigidBody.position + transform.up * 0.1f, _sphereCollider.radius, -transform.up, out RaycastHit hit, 0.2f);
        Debug.Log("Gounded: " + _grounded);
        if (_grounded)
        {
            Debug.Log("ok");
            // Rotate sidecar
            float steerInput = _gameplayActions.TempSteer.ReadValue<float>();
            transform.Rotate(transform.up, steerInput * _turnSpeed * Time.deltaTime);
            // Apply engine force into the sidecar direction
            float accelInput = _gameplayActions.TempAccelerate.ReadValue<float>();
            float breakInput = _gameplayActions.TempBrake.ReadValue<float>();
            if (accelInput > 0)
            {
                _rigidBody.AddForce(transform.forward * (_accelForce * accelInput), ForceMode.Acceleration);
                // ForceMode.Acceleration: interprets the parameter as acceleration and changes the velocity by the value of force * DT.
                // The effect depends on the simulation step lenght but doesn't depend on the mass of the body.
            }
            if (breakInput > 0)
            {
                _rigidBody.AddForce(-transform.forward * (_brakeForce * breakInput), ForceMode.Acceleration);
            }
            Vector3 currentVelocity = _rigidBody.linearVelocity;
            Vector3 targetVelocity = transform.forward * currentVelocity.magnitude;
            // if _gripFactor = 1f -> the velocity of the sphere tends to targetVelocity, this means that the direction of the velocity of the sphere is instanlty equals to the direction of the sidecar. So the sidecar doesn't derapa.
            // if _gripFactor = 0.1f -> the velocity of the sphere tends to currentVelocity, this means that the direction of the velocity of the sphere slowly tends to be equals to the direction of the sidecar. So the sidecar derapa.
            // TODO magari il smoothness coefficient lo faccio diventare una variabile
            // 5f is the smoothness coefficient that determins how fast or slow the velocity of the sphere tends to targetVelocity
            _rigidBody.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, _gripFactor * Time.fixedDeltaTime * 5f);
            // Allignement of the sphere to the ground
            Vector3 fwd = Vector3.Cross(transform.right, hit.normal);
            Quaternion targetRotation = Quaternion.LookRotation(fwd, hit.normal);
            transform.rotation = Quaternion.LookRotation(fwd, hit.normal);
        }
    }
}
