using UnityEngine;

public class SphereGizmo : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private bool _isRbEnabled;
    [SerializeField] private Transform _parent;
    [SerializeField] private bool _isParentEnabled;
    [SerializeField] private Transform _sidecar;
    [SerializeField] private bool _isSidecarEnabled;
    [SerializeField] private float _visualScale = 0.5f;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (_isRbEnabled)
            Gizmos.DrawRay(transform.position, _rb.linearVelocity * _visualScale);
        Gizmos.color = Color.red;
        if (_isParentEnabled)
            Gizmos.DrawRay(transform.position, _parent.forward * _visualScale);
        Gizmos.color = Color.orange;
        if (_isSidecarEnabled)
            Gizmos.DrawRay(transform.position, _sidecar.forward * _visualScale);
    }
}
