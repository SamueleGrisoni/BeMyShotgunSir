using System;
using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;

namespace BeMyShotgunSir.Scripts.UI
{
    public interface IDriverInputConsumer
    {
        int SteerInput { get; }
        bool IsDrifting { get; }
        bool IsMoving { get; }
        event Action OnBoostPressed;
        event Action OnEarlyCommitmentPressed;
    }

    public interface IShotgunInputConsumer
    {

    }

    public interface IInputPublisher
    {
        void SetSteerInput(int input);
        void SetIsDrifting(bool isDrifting);
        void SetIsMoving(bool isMoving);
        void PressBoost();
        void PressEarlyCommitment();
    }
    public class InputPublisher : IInputPublisher, IDriverInputConsumer, IShotgunInputConsumer
    {
        #region Driver

        private int _steerInput;
        private bool _isDrifting;
        private bool _isMoving;
        public event Action OnBoostPressed;
        public event Action OnEarlyCommitmentPressed;

        public int SteerInput => _steerInput;
        public bool IsDrifting => _isDrifting;
        public bool IsMoving => _isMoving;

        public void SetSteerInput(int input) => _steerInput = input;
        public void SetIsDrifting(bool isDrifting) => _isDrifting = isDrifting;
        public void PressBoost() => OnBoostPressed?.Invoke();
        public void PressEarlyCommitment() => OnEarlyCommitmentPressed?.Invoke();
        public void SetIsMoving(bool isMoving) => _isMoving = isMoving;

        #endregion

        #region Shotgun

        public event Action<int> OnSelectInventoryPowerUp;
        public event Action<PowerUp> OnEquipPowerUp;
        public event Action OnFirePressed;
        public event Action<WheelMessages> OnWheelMessagePressed;

        public void SelectInventoryPowerUp(int index) => OnSelectInventoryPowerUp?.Invoke(index);
        public void EquipPowerUp(PowerUp powerUp) => OnEquipPowerUp?.Invoke(powerUp);
        public void PressFire() => OnFirePressed?.Invoke();
        public void PressWheelMessage(WheelMessages message) => OnWheelMessagePressed?.Invoke(message);

        #endregion
    }
}
