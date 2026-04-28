using System;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Transporting;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceNetController : NetController
    {
        private ServerManager _serverManager;
        private RaceManager _raceManager;
        public event Action OnRaceNetControllerSpawned;
        public event Action OnRaceNetControllerDespawned;

        private void Awake()
        {
            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            if (!TryGetComponent(out _raceManager))
                Log.ELazy(() => "No RaceManager found on the same GameObject.", this);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            OnRaceNetControllerSpawned?.Invoke();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _serverManager.OnRemoteConnectionState += HandleRemoteConnectionState;

            // InitSyncValues();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            //NOTE host operations must be written below this check, otherwise the host will execute them twice (once as server, once as client)
            if (!IsServerInitialized)
                return;
        }

        private void InitSyncValues() { }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            OnRaceNetControllerDespawned?.Invoke();
        }

        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (!IsServerInitialized)
                return;

            if (args.ConnectionState == RemoteConnectionState.Started)
                return;

            if (args.ConnectionState == RemoteConnectionState.Stopped)
                Despawn();
        }



    }
}
