using BeMyShotgunSir.Scripts.UI;
using FishNet.Object;
using UnityEngine.InputSystem;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public class DriverInput : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
            {
                GetComponent<PlayerInput>().enabled = true;
            }
        }
        public float SteerInput { get; private set; }
        public bool IsDrifting { get; private set; }
        public bool IsBoosting { get; private set; }
        public bool IsStarting { get; private set; }
        public CommitmentDirection EarlyCommitment { get; private set; }

        private void OnSteer(InputValue value) => SteerInput = value.Get<float>();
        private void OnStart(InputValue value) => IsStarting = value.isPressed;
        private void OnDrift(InputValue value) => IsDrifting = value.isPressed;
        private void OnBoost(InputValue value) => IsBoosting = value.isPressed;
        private void OnLeftEarlyCommitment(InputValue value)
        {
            if(value.isPressed)
            {
                EarlyCommitment = CommitmentDirection.Left;
            }
        }
        private void OnRightEarlyCommitment(InputValue value)
        {
            if (value.isPressed)
            {
                EarlyCommitment = CommitmentDirection.Right;
            }
        }
    }
}