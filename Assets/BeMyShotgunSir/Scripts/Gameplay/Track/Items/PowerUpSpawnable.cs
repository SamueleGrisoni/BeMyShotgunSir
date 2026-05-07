using System;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public interface IPowerUpSpawnable
    {
        PowerUp PowerUpType { get; }
        void Initialize(PowerUpsNetController powerUpsNetController);
    }

    public class PowerUpSpawnable : NetworkBehaviour, IPowerUpSpawnable
    {
        public static event Action<IPowerUpSpawnable> OnPowerUpSpawned;
        [SerializeField] private PowerUp _powerUpType;
        public PowerUp PowerUpType => _powerUpType;
        private PowerUpsNetController _netController;
        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField] private float _bobHeight = 0.2f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private GameObject _itemVisualsPrefabs;

        private float _startLocalY;
        private float _minY;

        public override void OnStartServer()
        {
            base.OnStartServer();
            OnPowerUpSpawned?.Invoke(null);
        }

        private void Start()
        {
            _startLocalY = _itemVisualsPrefabs.transform.localPosition.y;
            _minY = _startLocalY;
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

        public void Initialize(PowerUpsNetController powerUpsNetController)
        {
            if (_netController != null)
                return;
            _netController = powerUpsNetController;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (IsClientInitialized)
            {
                //SOUND here
            }
            if (!IsServerInitialized)
                return;
            if (other.CompareTag("Driver"))
            {
                if (other.TryGetComponent(out IDriverController driverController))
                {
                    int teamId = driverController.GetTeam();
                    _netController.AddPowerUpToTeam(teamId, PowerUpType);
                    NetworkObject.Despawn();
                }
            }
        }
    }
}
