namespace BeMyShotgunSir.Scripts.UI
{
    public class UIFlowState
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
