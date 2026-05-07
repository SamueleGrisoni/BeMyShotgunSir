using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public interface IPowerUpStrategy
    {
        bool CanActivate(PowerUpRuntime runtime);
        void Activate(PowerUpRuntime runtime);
        void OnTick(PowerUpRuntime runtime, float deltaTime);
        void Deactivate(PowerUpRuntime runtime);
        void Expire(PowerUpRuntime runtime);
    }
    public abstract class PowerUpDefinition : ScriptableObject, IPowerUpStrategy
    {
        public PowerUp PowerUp;
        public PowerUpClass Type;
        public string Name;
        public float Duration;
        public bool IsAllowedDuringAim;

        public void Activate(PowerUpRuntime runtime) => throw new System.NotImplementedException();
        public bool CanActivate(PowerUpRuntime runtime) => throw new System.NotImplementedException();
        public void Deactivate(PowerUpRuntime runtime) => throw new System.NotImplementedException();
        public void Expire(PowerUpRuntime runtime) => throw new System.NotImplementedException();
        public void OnTick(PowerUpRuntime runtime, float deltaTime) => throw new System.NotImplementedException();
    }
}
