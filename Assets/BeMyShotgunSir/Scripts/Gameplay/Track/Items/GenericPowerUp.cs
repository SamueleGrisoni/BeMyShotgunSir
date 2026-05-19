using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public class GenericPowerUp : NetworkBehaviour
    {
        public static event Action<GenericPowerUp> OnPowerUpSpawned;
        private readonly SyncVar<PowerUp> _syncPowerUpType = new SyncVar<PowerUp>(PowerUp.None);
        public PowerUp PowerUpType => _syncPowerUpType.Value;
        [SerializeField] private List<PowerUpSpawnable> _spawnablePrefabs;
        public int? ChunkIndex { get; private set; }

        private PowerUpsNetController _netController;
        private RaceNetStateStore _raceNetStateStore;
        private PowerUpSpawnable _currentSpawnable;
        private TriggerRelay _triggerRelay;

        public void SetChunkIndex(int chunkIndex)
        {
            if (!ChunkIndex.HasValue)
                ChunkIndex = chunkIndex;
        }

        public void Initialize(PowerUpsNetController powerUpsNetController, RaceNetStateStore raceNetStateStore)
        {
            if (_netController != null)
                return;
            _netController = powerUpsNetController;
            _raceNetStateStore = raceNetStateStore;
        }

        [Server(UseIsStarted = true)]
        public void SetPowerUpType(PowerUp powerUpType) => _syncPowerUpType.Value = powerUpType;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _syncPowerUpType.OnChange += OnPowerUpTypeChanged;
            // PowerUpSpawnable[] mySpawnables = GetComponentsInChildren<PowerUpSpawnable>(includeInactive: true);
            // _spawnablePrefabs = new List<PowerUpSpawnable>(mySpawnables);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _syncPowerUpType.OnChange -= OnPowerUpTypeChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            OnPowerUpSpawned?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_syncPowerUpType.Value != PowerUp.None)
                OnPowerUpTypeChanged(PowerUp.None, _syncPowerUpType.Value, false);
        }

        private void OnPowerUpTypeChanged(PowerUp _, PowerUp next, bool asServer)
        {
            foreach (PowerUpSpawnable spawnable in _spawnablePrefabs)
            {
                bool isMatch = spawnable.PowerUpType == next;
                if (isMatch)
                {
                    spawnable.gameObject.SetActive(true);
                    _currentSpawnable = spawnable;
                    spawnable.SetVisualsEnabled(true);
                    _triggerRelay = spawnable.GetComponentInChildren<TriggerRelay>();
                    if (_triggerRelay is null)
                    {
                        Log.ELazy(() => $"No TriggerRelay found in children of spawnable {spawnable.name}. Power-up pickup will not work.", this);
                    }
                    else
                    {
                        _triggerRelay.Init(this);
                    }
                }
                else
                {
                    if (spawnable.gameObject.activeSelf)
                    {
                        spawnable.SetVisualsEnabled(false);
                        spawnable.gameObject.SetActive(false);
                    }
                }
            }
        }

        internal void OnChildTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Driver"))
                return;

            // Sound can go here (runs on every peer)

            if (IsServerInitialized)
                HandleTriggerServer(other);
            else if (IsClientInitialized)
                HandleTriggerClient(other);
        }

        [Server]
        private void HandleTriggerServer(Collider other)
        {
            if (!IsServerInitialized) return;

            if (other.TryGetComponent(out MovementController movementController))
            {
                int? teamId = movementController.TeamId;
                if (teamId.HasValue)
                {
                    if (_netController.AddPowerUpToTeam(teamId.Value, PowerUpType))
                        NetworkObject.Despawn();
                }
                else
                    Log.ELazy(() => $"Driver has no team assigned. Power-up pickup failed. Driver: {other.gameObject.name}", this);
            }
        }

        [Client]
        private void HandleTriggerClient(Collider other)
        {
            if (!IsClientInitialized) return;

            if (other.TryGetComponent(out MovementController movementController))
            {
                int? teamId = movementController.TeamId;
                if (teamId.HasValue)
                {
                    if (_raceNetStateStore.TryGetInventoryIsFull(teamId.Value, out bool isFull) && !isFull)
                    {
                        if (_currentSpawnable != null)
                            _currentSpawnable.SetVisualsEnabled(false);
                    }
                }
                else
                    Log.ELazy(() => $"Driver has no team assigned. Power-up pickup failed. Driver: {other.gameObject.name}", this);
            }
        }
    }
}
