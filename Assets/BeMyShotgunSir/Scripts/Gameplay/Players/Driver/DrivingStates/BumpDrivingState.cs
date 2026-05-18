using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class BumpDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.ApplyBump(controller.AnimationStats.BumpForce, -controller.SidecarForward);
            controller.BumpTimer = 0f;

            if (!isReplayed)
                controller.BumpSoundEffect();
        }
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.BumpTimer += controller.TickDelta();
            if (controller.BumpTimer > controller.AnimationStats.BumpStunDuration)
            {
                controller.ChangeState(controller.NormalState, data, isReplayed);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.ApplyAcceleration(controller.SidecarForward, 0f, 0f);
            float steeringDirection = controller.CommitmentColliderLayer == LayerMask.NameToLayer("RightCollider") ? -1 : 1;
            controller.ApplySteering(steeringDirection, controller.AnimationStats.RotationPerTick);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => "Exiting bump state", this);
    }
}