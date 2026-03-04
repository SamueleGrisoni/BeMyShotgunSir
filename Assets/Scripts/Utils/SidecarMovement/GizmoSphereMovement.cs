using UnityEngine;

public class GizmoSphereMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private float _visualScale = 0.5f;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, _rb.linearVelocity * _visualScale);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, _rb.linearVelocity.magnitude * transform.forward);
        Gizmos.color = Color.violet;
        // Gizmos.DrawRay(transform.position, Vector3.right * 50f);
        // Gizmos.DrawRay(transform.position, Vector3.left * 50f);
    }
}
