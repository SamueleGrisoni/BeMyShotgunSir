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
        private bool _log = true;
        private ServerManager _serverManager;
        private IRaceManager_NetController _raceManager;
        public static event Action<IRaceNetController> OnRaceNetControllerReady;
        public static event Action OnRaceNetControllerDespawned;

        private void OnEnable() =>
             RaceManager.OnRaceManagerStarted += OnRaceManagerStarted;

        public override void OnStartNetwork() =>
            base.OnStartNetwork();

        private void OnRaceManagerStarted(IRaceManager manager)
        {
            if (manager is not IRaceManager_NetController manager_NetController)
                return;

            if (_raceManager != null)
            {
                Log.ELazy(() => "RaceManager reference is already set. Multiple RaceManagers are not supported.", this);
                return;
            }

            _raceManager = manager_NetController;

            Log.DLazy(() => "RaceNetController is ready.", this, _log);
            OnRaceNetControllerReady?.Invoke(this);
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
            UnsubscribeEvents();
            Log.DLazy(() => "RaceNetController despawned from the network.", this, _log);
            OnRaceNetControllerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
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
