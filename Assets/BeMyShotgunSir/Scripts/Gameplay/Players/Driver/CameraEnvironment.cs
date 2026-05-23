using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;


namespace BeMyShotgunSir.Gameplay.Players.Driver
{
    public class CameraEnvironment : NetworkBehaviour
    {
        [SerializeField] private CinemachineCamera _targetCamera;
        private void LateUpdate()
        {
            this.transform.position = _targetCamera.transform.position;
        }
    }
}