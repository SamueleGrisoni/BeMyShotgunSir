using System;
using BeMyShotgunSir.Scripts.Gameplay.Messages;

namespace BeMyShotgunSir.Scripts.UI
{
    public enum CommitmentDirection
    {
        Left,
        Right,
        Default,
    }

    public interface IDriverInputConsumer
    {
        // To Driver
        float SteerInput { get; }
        float DriftInput { get; }
        bool IsDrifting { get; }
        bool IsMoving { get; }
        event Action OnBoostPressed;
        event Action<CommitmentDirection> OnEarlyCommitmentPressed;
        event Action<DriverFeedback> OnDriverFeedbackPressed;
        event Action<WheelMessages> OnWheelMessageOnDriver;
        // From Driver
        float ChargeBattery { set; }
        void BatteryChargeEarlyCommitment(float batteryCharge);
        void SendDriveFeedbackToShotgun(DriverFeedback driverFeedback);
    }

    public interface IShotgunInputConsumer
    {
        event Action<int> OnEquipPressed;
        event Action OnUsePressed;
        event Action OnFirePressed;
        event Action<WheelMessages> OnWheelMessagePressed;
        void SendWheelMessageToDriver(WheelMessages wheelMessages);
    }

    public interface IInputPublisher
    {
        //Driver
        void SetSteerInput(float input);
        void SetDriftInput(float input);
        void SetIsDrifting(bool isDrifting);
        void SetIsMoving(bool isMoving);
        void PressBoost();
        void PressEarlyCommitment(CommitmentDirection direction);
        void PressDriverFeedback(DriverFeedback feedback);
        //Shotgun
        void EquipPowerUp(int slot);
        void UsePowerUp();
        void Fire();
        void SendWheelMessage(WheelMessages message);

        float GetBatteryCharge();
        event Action<float> OnBatteryChargeEarlyCommitment;
    }
    public class InputPublisher : IInputPublisher, IDriverInputConsumer, IShotgunInputConsumer
    {
        #region Driver

        private float _steerInput;
        private float _driftInput;
        private bool _isDrifting;
        private bool _isMoving;
        private float _chargeBattery;
        public event Action OnBoostPressed;
        public event Action<CommitmentDirection> OnEarlyCommitmentPressed;
        public event Action<DriverFeedback> OnDriverFeedbackPressed;
        public event Action<float> OnBatteryChargeEarlyCommitment;
        public event Action<DriverFeedback> OnDriverSendFeedback;
        public event Action<WheelMessages> OnWheelMessageOnDriver;

        public float SteerInput => _steerInput;
        public float DriftInput => _driftInput;
        public bool IsDrifting => _isDrifting;
        public bool IsMoving => _isMoving;
        public float ChargeBattery { set => _chargeBattery = value; }


        public void SetSteerInput(float input) => _steerInput = input;
        public void SetDriftInput(float input) => _driftInput = input;
        public void SetIsDrifting(bool isDrifting) => _isDrifting = isDrifting;
        public void PressBoost() => OnBoostPressed?.Invoke();
        public void PressEarlyCommitment(CommitmentDirection direction) => OnEarlyCommitmentPressed?.Invoke(direction);
        public void PressDriverFeedback(DriverFeedback feedback) => OnDriverFeedbackPressed?.Invoke(feedback);
        public void SetIsMoving(bool isMoving) => _isMoving = isMoving;

        public float GetBatteryCharge() => _chargeBattery;
        public void BatteryChargeEarlyCommitment(float batteryCharge) => OnBatteryChargeEarlyCommitment?.Invoke(batteryCharge);
        public void SendDriveFeedbackToShotgun(DriverFeedback driverFeedback) => OnDriverSendFeedback?.Invoke(driverFeedback);


        #endregion

        #region Shotgun

        public event Action<int> OnEquipPressed;
        public event Action OnUsePressed;
        public event Action OnFirePressed;
        public event Action<WheelMessages> OnWheelMessagePressed;

        public void EquipPowerUp(int slot) => OnEquipPressed?.Invoke(slot);
        public void UsePowerUp() => OnUsePressed?.Invoke();
        public void Fire() => OnFirePressed?.Invoke();
        public void SendWheelMessage(WheelMessages message) => OnWheelMessagePressed?.Invoke(message);
        public void SendWheelMessageToDriver(WheelMessages wheelMessages) => OnWheelMessageOnDriver?.Invoke(wheelMessages);

        #endregion
    }
}
