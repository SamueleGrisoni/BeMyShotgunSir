using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceNetData : INetData { }
    public interface IRaceData : IData { }
    public interface IRaceDataView : IDataView, ILobbyDataView, IRaceData, IRaceNetData
    {
        // Define events for data changes if needed
    }

    public class RaceViewModel : ViewModel<IRaceData, IRaceNetData>, IRaceDataView
    {
        public LobbyInfo LobbyInfo { get; private set; }
        public int PlayerCount { get; private set; }
        public Dictionary<int, PlayerLobbyState> PlayerStates { get; private set; }

        public event Action OnLobbyInfoChanged;
        public event Action OnLobbyIPChanged;
        public event Action OnPlayerCountChanged;
        public event Action OnPlayerStatesChanged;

        public override void InitData(IRaceData data = null)
        {
            if (data == null)
                return;

            if (data is not LobbyViewModel lobbyData)
            {
                Log.ELazy(() => "InitData called with invalid data type. Expected SOLobbyData.", this);
                return;
            }
            LobbyInfo = lobbyData.LobbyInfo;
            PlayerCount = lobbyData.PlayerCount;
            PlayerStates = lobbyData.PlayerStates;
        }

        public override void InitNetData(IRaceNetData data)
        {
            if (data is not IRaceNetData raceNetData)
            {
                Log.ELazy(() => "InitNetData called with invalid data type. Expected IRaceNetData.", this);
                return;
            }
            //no setters to allow a real refresh even for unchanged values
        }
    }
}
