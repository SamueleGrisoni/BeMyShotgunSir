using UnityEngine;

public class DriverCameraController : MonoBehaviour
{
    [SerializeField] private GameObject _sphereMovement;
    // TODO variabile da rinominare
    [SerializeField] private GameObject _sidecarContainer;
    [SerializeField] private Vector3 _cameraOffset;
    [SerializeField] private float _smoothnessCoefficient = 0.5f;
    private Rigidbody _rbSphereMovement;
    private void Awake() => _rbSphereMovement = _sphereMovement.GetComponent<Rigidbody>();

    private void LateUpdate()
    {
        transform.position = _sphereMovement.transform.position + _cameraOffset;

        if (_rbSphereMovement.linearVelocity.sqrMagnitude > 0.01f)
        {
            var rotSphereLinearVelocity = Quaternion.LookRotation(_rbSphereMovement.linearVelocity.normalized);
            var rotSidecarDirection = Quaternion.LookRotation(_sidecarContainer.transform.forward);
            transform.rotation = Quaternion.Slerp(rotSphereLinearVelocity, rotSidecarDirection, _smoothnessCoefficient);
        }
    }
}
