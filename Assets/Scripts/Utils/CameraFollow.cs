using UnityEngine;
using UnityEngine.Serialization;

public class CameraFollow : MonoBehaviour
{
    public Transform _target;
    public Vector3 _offset;

    private void LateUpdate()
    {
        transform.position = _target.position + _offset;
        transform.LookAt(_target);
    }

    private void OnValidate()
    {
        transform.position = _target.position + _offset;
        transform.LookAt(_target);
    }
}
