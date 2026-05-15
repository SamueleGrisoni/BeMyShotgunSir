using System;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public class PowerUpSpawnable : NetworkBehaviour
    {
        public static event Action<PowerUpSpawnable> OnPowerUpSpawned;
        public int? ChunkIndex { get; private set; }
        public void SetChunkIndex(int chunkIndex)
        {
            if (!ChunkIndex.HasValue)
                ChunkIndex = chunkIndex;
        }

        [SerializeField] private PowerUp _powerUpType;
        private readonly SyncVar<PowerUp> _syncPowerUpType = new SyncVar<PowerUp>(PowerUp.None);
        public PowerUp PowerUpType => _syncPowerUpType.Value;
        private PowerUpsNetController _netController;
        private RaceNetStateStore _raceNetStateStore;
        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField] private float _bobHeight = 0.2f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private GameObject _itemVisualsPrefabs;

        private float _startLocalY;
        private float _minY;

        private void UpdatePowerUpType(PowerUp prev, PowerUp next, bool asServer)
        {
            if (asServer)
                return;
            //TODO Here we could update the visuals based on the power-up type if needed
        }

        private void Start()
        {
            _startLocalY = _itemVisualsPrefabs.transform.localPosition.y;
            _minY = _startLocalY;
        }

        [Server]
        public void SetPowerUpType(PowerUp powerUpType) => _syncPowerUpType.Value = _powerUpType;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _syncPowerUpType.OnChange += UpdatePowerUpType;

        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            _syncPowerUpType.Value = _powerUpType;
            OnPowerUpSpawned?.Invoke(this);
        }


        private void Update()
        {
            _itemVisualsPrefabs.transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f, Space.World);
            Vector3 localPos = _itemVisualsPrefabs.transform.localPosition;
            localPos.y = Mathf.Max(_minY, _startLocalY + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
            _itemVisualsPrefabs.transform.localPosition = localPos;
        }

        private void OnDrawGizmos()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
            }
        }

        public void Initialize(PowerUpsNetController powerUpsNetController, RaceNetStateStore raceNetStateStore)
        {
            if (_netController != null)
                return;
            _netController = powerUpsNetController;
            _raceNetStateStore = raceNetStateStore;
        }

        [Server]
        public void OnTriggerEnterServer(Collider other)
        {
            if (!IsServerInitialized)
                return;
            if (other.CompareTag("Driver"))
            {
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
        }

        [Client]
        public void OnTriggerEnterClient(Collider other)
        {
            if (!IsClientInitialized)
                return;
            if (other.CompareTag("Driver"))
            {
                if (other.TryGetComponent(out MovementController movementController))
                {
                    int? teamId = movementController.TeamId;
                    if (teamId.HasValue)
                    {
                        if (_raceNetStateStore.TryGetInventoryIsFull(teamId.Value, out bool isFull) && !isFull)
                            GetComponent<MeshRenderer>().enabled = false;
                    }
                    else
                        Log.ELazy(() => $"Driver has no team assigned. Power-up pickup failed. Driver: {other.gameObject.name}", this);
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Driver"))
            {
                //SOUND here
            }
            if (IsServerInitialized)
            {
                OnTriggerEnterServer(other);
                return;
            }
            if (IsClientInitialized)
                OnTriggerEnterClient(other);
        }
    }
}
