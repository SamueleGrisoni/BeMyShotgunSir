using System;
using UnityEngine;

public class TempCameraController : MonoBehaviour
{
    [SerializeField] private GameObject _sphereMovement;
    [SerializeField] private Vector3 _cameraOffset;

    private void LateUpdate()
    {
        transform.position = _sphereMovement.transform.position + _cameraOffset;
    }
}
