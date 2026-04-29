using System;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;

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
        private bool _log = true;
        private LobbyViewModel _lobbyViewModel;
        public ILobbyDataView LobbyDataView => _lobbyViewModel;

        public event Action OnLobbyInfoChanged;
        public event Action OnLobbyIPChanged;
        public event Action OnPlayerCountChanged;
        public event Action OnPlayerStatesChanged;

        public override void InitData(IRaceData data = null)
        {
            Log.DLazy(() => "Initializing RaceViewModel data.", this, _log);
        }

        public void InitData(LobbyViewModel lobbyViewModel, IRaceData raceData = null)
        {
            _lobbyViewModel = lobbyViewModel;
            InitData(raceData);
            Log.DLazy(() => "Including LobbyViewModel data in RaceViewModel Initialization.", this);
        }

        public override void InitNetData(IRaceNetData data)
        {
            Log.DLazy(() => "Initializing RaceViewModel net data.", this, _log);
            //no setters to allow a real refresh even for unchanged values
        }
    }
}
