using BeMyShotgunSir.Core;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class SORaceData : SOData, IRaceData
    {
        private SOLobbyData _lobbyData;
        public override void InitData(SOData data = null)
        {
            if (data is not SOLobbyData lobbyData)
            {
                Log.ELazy(() => "SORaceData: InitData called with invalid data type. Expected SOLobbyData.", this);
                return;
            }
            _lobbyData = lobbyData;
        }

        public override void InitNetData(INetData data)
        {
            if (data is not IRaceNetData raceNetData)
            {
                Log.ELazy(() => "SORaceData: InitNetData called with invalid data type. Expected IRaceNetData.", this);
                return;
            }
            //no setters to allow a real refresh even for unchanged values
        }
    }
}
