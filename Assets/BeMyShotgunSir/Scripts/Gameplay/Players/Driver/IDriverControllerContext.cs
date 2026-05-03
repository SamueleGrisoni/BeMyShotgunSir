using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDriverControllerContext
    {
        Vector3 ParentForward { get; }
        Vector3 SidecarForward { get; }
        Quaternion SidecarLocalRotation { get; set; }
        SOSidecarStats NormalStats { get; }
        SOSidecarStats BoostStats { get; }
        SOSidecarStats GrassStats { get; }
        SOBatteryStats BatteryStats { get; }
        IDrivingState IdleState { get; }
        IDrivingState NormalState { get; }
        IDrivingState DriftingState { get; }
        IDrivingState BoostState { get; }
        IDrivingState GrassState { get; }
        IDrivingState OilState { get; }
        float CurrentMaxSpeed { get; }
        float CurrentAcceleration { get; set; }
        bool IsGrounded { get; }
        bool IsDriftingButtonPressed { get; }
        float SteerInput { get; }
        bool IsBoostButtonPressed { get; }
        bool IsStartButtonPressed { get; }
        float DriftDirection { get; set; }
        float CurrentBatteryCharge { get; set; }
        float BatteryChargeTimer { get; set; }
        float BoostTimer { get; set; }
        void ChangeState(IDrivingState state, ReplicateData data);
        void SetMaxSpeed(float maxSpeed); // TODO spostare in CUrrentMaxSpeed 
        void SetDriftDirection(float driftDirection);
        void ApplyAcceleration(Vector3 direction, float accelerationForce);
        void ApplyGravity(float gravity);
        void ApplySteering(float steerAmount, float steeringForce);
        void ApplyVisualRotation(Quaternion targetRot, float steerAngularRotationSlerp);
        void ApplyLateralGrip(float lateralGripFactor);
        void AnimateSidecar();
        GroundType CheckGround();
        float TickDelta();
        bool IsOnwer { get; }
        bool IsServer { get; }
    }
}
