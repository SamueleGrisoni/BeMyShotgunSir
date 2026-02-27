using UnityEngine;

public class SphereContactPoint : MonoBehaviour
{
    [SerializeField] private GameObject _sphereContactPoint;
    private float _sphereRadius;

    void Start()
    {
        _sphereRadius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
    }

    void FixedUpdate()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                _sphereRadius + 0.1f))
        {
            _sphereContactPoint.transform.position = hit.point;
        }
    }
}
