using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.UI;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceNetContext
    {
        public LobbyNetContext LobbyNetContext { get; }
        public IRaceManager Manager { get; }
        public IRaceNetController NetController { get; }
        public RaceNetStateStore NetState { get; }
        public RaceClientProjector ClientProjector { get; }
        public PowerUpsNetController PowerUpsNetController { get; }
        public InputPublisher InputPublisher { get; }

        public RaceNetContext(LobbyNetContext lobbyNetContext, IRaceManager manager, IRaceNetController netController, RaceNetStateStore netState, RaceClientProjector clientProjector, PowerUpsNetController powerUpsNetController, InputPublisher inputPublisher)
        {
            LobbyNetContext = lobbyNetContext;
            Manager = manager;
            NetController = netController;
            NetState = netState;
            ClientProjector = clientProjector;
            PowerUpsNetController = powerUpsNetController;
            InputPublisher = inputPublisher;
        }
    }
}
