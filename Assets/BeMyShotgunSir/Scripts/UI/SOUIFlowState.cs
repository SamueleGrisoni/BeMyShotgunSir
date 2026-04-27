using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    public class SOUIFlowState : ScriptableObject
    {
        private UIScreen _nextState;
        public void SetNextState(UIScreen nextState) =>
            _nextState = nextState;

        public UIScreen GetNextState()
        {
            UIScreen result = _nextState;
            _nextState = UIScreen.None;
            return result;
        }
    }
}
