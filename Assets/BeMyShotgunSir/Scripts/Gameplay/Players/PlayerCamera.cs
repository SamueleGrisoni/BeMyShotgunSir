using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Player.Driver
{
    public class PlayerCamera : NetworkBehaviour
    {
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        public override void OnStartClient()
        {
            base.OnStartClient();
            _cinemachineCamera.enabled = IsOwner;
        }
    }
}