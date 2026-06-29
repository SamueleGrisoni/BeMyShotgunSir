using Unity.Cinemachine;
using UnityEngine;


namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public class CameraEnvironment : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _targetCamera;
        private void LateUpdate()
        {
            this.transform.position = _targetCamera.transform.position;
        }
    }
}