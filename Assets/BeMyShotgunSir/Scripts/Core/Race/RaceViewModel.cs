using System;
using BeMyShotgunSir.Scripts.Core.Lobby;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceNetData : INetData { }
    public interface IRaceData : IData { }
    public interface IRaceDataView : IDataView, IRaceData, IRaceNetData
    {
        ILobbyDataView LobbyDataView { get; }
        // Define events for data changes if needed
    }

    public class RaceViewModel : ViewModel<IRaceData, IRaceNetData>, IRaceDataView
    {
        private LobbyViewModel _lobbyViewModel;
        public ILobbyDataView LobbyDataView => _lobbyViewModel;

        public event Action OnLobbyInfoChanged;
        public event Action OnLobbyIPChanged;
        public event Action OnPlayerCountChanged;
        public event Action OnPlayerStatesChanged;

        public override void InitData(IRaceData data = null) { }

        public void InitData(LobbyViewModel lobbyViewModel, IRaceData raceData = null)
        {
            _lobbyViewModel = lobbyViewModel;
            InitData(raceData);
        }

        public override void InitNetData(IRaceNetData data)
        {
            //no setters to allow a real refresh even for unchanged values
        }
    }
}
