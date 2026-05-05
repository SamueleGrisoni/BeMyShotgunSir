using System;

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
    }
}
