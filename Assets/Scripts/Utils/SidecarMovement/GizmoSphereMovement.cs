using System;
using UnityEngine;
using System.Collections.Generic;

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

    }
}
