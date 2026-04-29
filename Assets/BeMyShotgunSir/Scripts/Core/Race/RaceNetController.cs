using System;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    #region RaceNetController Interfaces
    public interface IRaceNetController_Command : INetController_Command { }
    public interface IRaceNetController_Manager : INetController_Manager { }
    public interface IRaceNetController : INetController, IRaceNetController_Command, IRaceNetController_Manager { }

    #endregion

    [RequireComponent(typeof(RaceManager))]
    public class RaceNetController : NetController, IRaceNetController
    {
        private ServerManager _serverManager;
        private IRaceManager_NetController _raceManager;
        public event Action OnRaceNetControllerSpawned;
        public event Action<IRaceNetController> OnRaceNetControllerReady;
        public event Action OnRaceNetControllerDespawned;

        private void OnEnable() =>
             RaceManager.OnRaceManagerSpawned += OnRaceManagerSpawned;
        private void OnRaceManagerSpawned(IRaceManager manager)
        {
            if (_raceManager != null)
            {
                Log.ELazy(() => "RaceManager reference is already set. Multiple RaceManagers are not supported.", this);
                return;
            }
            if (manager is not IRaceManager_NetController manager_NetController)
                return;

            _raceManager = manager_NetController;
            OnRaceNetControllerReady?.Invoke(this);
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

            InitSyncValues();
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

        [Server]
        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (!IsServerInitialized)
                return;

            if (args.ConnectionState == RemoteConnectionState.Started)
                return;

            if (args.ConnectionState == RemoteConnectionState.Stopped)
                return;
        }
    }
}
