namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceCommand : Command<IRaceNetController_Command>
    {
        public RaceCommand(IRaceNetController_Command netController) : base(netController) { }

        public void LeaveRace_CMRequest() { /*TODO*/ }

        public void GetInitSnapshot_Request() =>
            _netController.OnRefresh();

        public void SetReadyToRace_Request() =>
            _netController.SetReadyToRace_ServerRpc();

    }
}
