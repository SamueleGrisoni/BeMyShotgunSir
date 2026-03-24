using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    public class PlayerInput : NetworkBehaviour
    {
        private UnityEngine.InputSystem.PlayerInput _playerInputComponent;
        private PlayerController _playerController;
        private InputActionAsset _inputActionAsset;
        private InputActionMap _inGameActionMap;

        private Vector2 _moveDirection;
        private Vector2 _lookDirection;
        public Vector2 Move => _moveDirection;
        public Vector2 Look => _lookDirection;

        private void Awake()
        {
            TryGetComponent(out _playerInputComponent);
            TryGetComponent(out _playerController);

            _playerInputComponent.enabled = false;
            _inputActionAsset = _playerInputComponent.actions;
            _inGameActionMap = _inputActionAsset.FindActionMap("InGameControls");

        }

        private void OnEnable()
        {
            if (_inGameActionMap != null)
                _inGameActionMap.actionTriggered += OnActionTriggered;
        }


        private void OnDisable()
        {
            if (_inGameActionMap != null)
                _inGameActionMap.actionTriggered -= OnActionTriggered;
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                _playerInputComponent.enabled = true;
                if (_inGameActionMap != null)
                {
                    _inGameActionMap.actionTriggered += OnActionTriggered;
                    _inGameActionMap.Enable();
                }
            }
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            Debug.Log(context.action.name + " was triggered");

            if (context.action.name == "Move")
                _moveDirection = context.ReadValue<Vector2>();
            else if (context.action.name == "Look")
            {
                _lookDirection = context.ReadValue<Vector2>();
                Debug.Log("Look direction: " + _lookDirection);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (_playerController == null)
                TryGetComponent(out _playerController);
        }
    }
}
