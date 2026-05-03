using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public abstract class IDrivingState : MonoBehaviour
    {
        protected IDrivingState _previousState;
        public abstract void Enter(IDrivingState previousState, IDriverControllerContext controller, ReplicateData data);
        public abstract void CheckStateChange(IDriverControllerContext controller, ReplicateData data);
        public abstract void RunInputs(IDriverControllerContext controller, ReplicateData data);
        public abstract void Exit(IDriverControllerContext controller, ReplicateData data);
    }
}
