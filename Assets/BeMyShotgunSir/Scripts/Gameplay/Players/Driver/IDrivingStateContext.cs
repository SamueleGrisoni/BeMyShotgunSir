using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDrivingStateContext
    {
        bool Log { get; }
        Vector3 ParentForward { get; }
        Vector3 SidecarForward { get; }
        LayerMask CommitmentColliderLayer { get; }
        SOSidecarStats NormalStats { get; }
        SOSidecarStats BoostStats { get; }
        SOSidecarStats GrassStats { get; }
        SOBatteryStats BatteryStats { get; }
        SOSidecarAnimationStats AnimationStats { get; }
        IDrivingState PreviousDrivingState { get; }
        IDrivingState IdleState { get; }
        IDrivingState NormalState { get; }
        IDrivingState DriftingState { get; }
        IDrivingState BoostState { get; }
        IDrivingState GrassState { get; }
        IDrivingState OilState { get; }
        IDrivingState BumpState { get; }
        float DriftDirection { get; set; }
        float CurrentBatteryCharge { get; set; }
        float BatteryChargeTimer { get; set; }
        float BoostTimer { get; set; }
        float OilAnimationTimer { get; set; }
        bool IsOilAnimationActive { get; set; }
        float BumpTimer { get; set; }
        CommitmentInfo CommitmentInfo { get; }
        void ChangeState(IDrivingState state, ReplicateData data, bool isReplayed);
        void ApplyAcceleration(Vector3 direction, float accelerationForce, float maxSpeed);
        void ApplySteering(float steerAmount, float steeringForce);
        void ApplyVisualRotation(Quaternion targetRot, float steerAngularRotationSlerp);
        void ApplyLateralGrip(float lateralGripFactor);
        void OilAnimation();
        bool CheckForkBarrierCollision();
        void ApplyBump(float bumpForce, Vector3 direction);
        GroundType CheckGround();
        float TickDelta();
        bool IsOnwer { get; }
        bool IsServer { get; }
    }
}
