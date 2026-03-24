using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;
using BeMyShotgunSir.Scripts.Core.Lobby;

namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private CinemachineCamera _playerCamera;
        [SerializeField] private PlayerUI _playerUI;
        private LobbyManager _lobbyManager;
        public void SetLobbyManager(LobbyManager lobbyManager)
        {
            if (_lobbyManager == null)
                _lobbyManager = lobbyManager;
        }
        private PlayerInput _playerInput;
        [SerializeField] private float _moveSpeed = 5f;

        private void Awake()
        {
            TryGetComponent(out _playerInput);
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                _playerCamera = Instantiate(_playerCamera);
                _playerUI = Instantiate(_playerUI);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;
            Move();
        }

        private void Move()
        {
            var inputDirection = new Vector3(_playerInput.Move.x, 0, _playerInput.Move.y);
            Vector3 move = inputDirection * _moveSpeed * Time.deltaTime;
            transform.Translate(move, Space.World);
        }

        private void Look()
        {
            Debug.Log("Look direction: " + _playerInput.Look);
        }

    }
}
