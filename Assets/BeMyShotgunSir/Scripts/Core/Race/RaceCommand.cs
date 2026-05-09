namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceCommand : Command<IRaceNetController_Command>
    {
        public RaceCommand(IRaceNetController_Command netController) : base(netController) { }

        public void LeaveRace_CMRequest() { /*TODO*/ }

        public void SetReadyToRace_Request() =>
            _netController.SetReadyToRace_ServerRpc();

    }
}
