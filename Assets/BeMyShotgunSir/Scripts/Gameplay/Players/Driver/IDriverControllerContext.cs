using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDriverControllerContext
    {
        Transform ParentTransform { get; } // TODO da capire se serve
        Transform SidecarTransform { get; }
        Vector3 ParentForward { get; }
        Vector3 SidecarForward { get; }
        SOSidecarStats Stats { get; }
        IDrivingState IdleState { get; }
        IDrivingState NormalState { get; }
        IDrivingState DriftingState { get; }
        IDrivingState AirState { get; }
        IDrivingState BoostState { get; }
        float CurrentMaxSpeed { get; }
        bool IsGrounded { get; }
        bool IsDriftingButtonPressed { get; }
        float SteerInput { get; }
        bool IsBoostButtonPressed { get; }
        bool IsStartButtonPressed { get; }
        float DriftDirection { get; set; }
        float CurrentBatteryCharge { get; set; }
        void ChangeState(IDrivingState state);
        void SetMaxSpeed(float maxSpeed);
        void ApplyAcceleration(Vector3 direction);
        void ApplyGravity(float gravity);
        void ApplySteering(float steerAmount);
        void ApplyVisualRotation(Quaternion targetRot);
        void ApplyLateralGrip();
        void AnimateSidecar();
        void ApplyBoost(float amount, float duration);
    }
}
