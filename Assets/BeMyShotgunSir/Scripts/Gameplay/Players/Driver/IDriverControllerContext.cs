using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDriverControllerContext
    {
        Transform ParentTransform { get; }
        Transform SidecarTransform { get; }
        SOSidecarStats Stats { get; }
        IDrivingState NormalState { get; }
        IDrivingState DriftingState { get; }
        IDrivingState AirState { get; }
        IDrivingState BoostState { get; }
        float CurrentMaxSpeed { get; }
        bool IsGrounded { get; }
        bool IsDriftingButtonPressed { get; }
        float SteerInput { get; }
        bool IsBoostButtonPressed { get; }
        float DriftDirection { get; set; }
        float CurrentBatteryCharge { get; set; }
        void ChangeState(IDrivingState state);
        void SetMaxSpeed(float maxSpeed);
        void ApplyAcceleration(Vector3 direction);
        void ApplyGravity(float gravity);
        void ApplySteering(float steerAmount);
        void ApplyLateralGrip(Vector3 direction);
        void AnimateSidecar(Quaternion targetRot);
        void ApplyBoost(float amount, float duration);
    }
}
