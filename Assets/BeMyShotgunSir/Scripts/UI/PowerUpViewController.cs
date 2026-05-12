using System.Collections;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class PowerUpViewController : RaceBindTarget
    {
        [SerializeField] private UIDocument _hudDocument;
        [SerializeField] private SOPowerUpIcons _powerUpIcons;
        [SerializeField] private float _swipeThreshold = 80f;


        #region Bindings
        private RaceCommand _command;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
        private IInputPublisher _inputPublisher;
        private RaceRole _role;
        #endregion

        public override void OnInitialBindComplete()
        {
            if (_initialBindSource == null)
            {
                Log.ELazy(() => "Initial bind source is null. Cannot complete initial bind.", this);
                return;
            }
            _command = _initialBindSource.Command;
            _viewModel = _initialBindSource.ViewModel;

            SubscribeViewModelEvents();
        }
        public override void OnFinalBindComplete()
        {
            if (_finalBindSource == null)
            {
                Log.ELazy(() => "Final bind source is null. Cannot complete final bind.", this);
                return;
            }
            _roadManager = _finalBindSource.RoadManager;
            _inputPublisher = _finalBindSource.InputPublisher;
            _role = _finalBindSource.Role;


        }

        #region Visual Elements
        private VisualElement _root;
        private TemplateContainer _powerUpTemplate;
        private VisualElement _powerUpList;
        private VisualElement _powerUp1;
        private VisualElement _powerUp2;
        private VisualElement _powerUp3;
        private VisualElement _powerUp4;
        private VisualElement _powerUp5;
        private VisualElement[] _powerUps;
        #endregion

        #region private fields
        private Vector2 _pointerDownPos;
        private bool _isPointerDown;
        private int _teamId;
        #endregion

        #region Debug
        private VisualElement _test1;
        private VisualElement _test2;
        private VisualElement _test3;
        #endregion

        private void OnEnable()
        {
            _root = _hudDocument.rootVisualElement;
            _test1 = _root.Q<VisualElement>("PowerUpBarContainer");
            _powerUpTemplate = _test1.Q<TemplateContainer>("PowerUpBar");
            _powerUpList = _powerUpTemplate.Q<VisualElement>("PowerUpList");
            _powerUp1 = _powerUpList.Q<VisualElement>("PowerUp1");
            _powerUp2 = _powerUpList.Q<VisualElement>("PowerUp2");
            _powerUp3 = _powerUpList.Q<VisualElement>("PowerUp3");
            _powerUp4 = _powerUpList.Q<VisualElement>("PowerUp4");
            _powerUp5 = _powerUpList.Q<VisualElement>("PowerUp5");

            _powerUps = new[] { _powerUp1, _powerUp2, _powerUp3, _powerUp4, _powerUp5 };

            StartCoroutine(InitNextFrame());

            InitPowerUpBar();
            Log.DLazy(() => $"OnEnable PowerUpVC instance: {GetInstanceID()}", this);
        }

        IEnumerator InitNextFrame()
        {
            yield return null;

            for (int i = 0; i < _powerUps.Length; i++)
            {
                int index = i;
                RegisterPowerUpSwipe(_powerUps[index], index);
            }
        }

        private void OnDisable()
        {
            _viewModel.OnInventoryChanged -= UpdatePowerUpBar;
        }

        private void SubscribeViewModelEvents()
        {
            if (_viewModel == null)
                return;
            _viewModel.OnInventoryChanged -= UpdatePowerUpBar;
            _viewModel.OnInventoryChanged += UpdatePowerUpBar;
            Log.DLazy(() => $"Subscribe PowerUpVC instance: {GetInstanceID()}", this);

        }

        private void RegisterPowerUpSwipe(VisualElement powerUpElement, int index)
        {
            powerUpElement.RegisterCallback<PointerDownEvent>(evt =>
            {
                Log.DLazy(() => $"Power-up {index + 1} pointer down", this);
                _command.EquipPowerUp(index);

                _isPointerDown = true;
                _pointerDownPos = evt.position;
                powerUpElement.CapturePointer(evt.pointerId);
            });

            powerUpElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!_isPointerDown)
                    return;
                _isPointerDown = false;

                Vector2 pointerUpPos = evt.position;
                Vector2 delta = pointerUpPos - _pointerDownPos;
                powerUpElement.ReleasePointer(evt.pointerId);

                if (delta.magnitude < _swipeThreshold)
                    return;

                if (delta.y < 0)
                {
                    Log.DLazy(() => $"Power-up {index + 1} swipe up detected. Requesting use of power-up.", this);
                    _command.ActivatePowerUp();
                }
            });

            powerUpElement.RegisterCallback<PointerCancelEvent>(evt =>

            {
                _isPointerDown = false;

                if (powerUpElement.HasPointerCapture(evt.pointerId))
                    powerUpElement.ReleasePointer(evt.pointerId);
            });
        }

        private void SelectPowerUp(int index)
        {
            Log.DLazy(() => $"Power-up {index + 1} selected", this);
            for (int i = 0; i < _powerUps.Length; i++)
            {
                if (i == index)
                    _powerUps[i].AddToClassList("selected");
                else
                    _powerUps[i].RemoveFromClassList("selected");
            }
        }

        private void UpdatePowerUpBar()
        {
            Log.DLazy(() => "Updating power-up bar UI", this);
            _viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId);
            if (teamId == null || !_viewModel.TeamInventories.TryGetValue(teamId.Value, out InventoryData inventory))
                return;

            if (_powerUps == null)
                return;

            for (int i = 1; i <= _powerUps.Length; i++)
            {
                PowerUp powerUp = inventory.GetPowerUpInSlot(i);

                if (powerUp != PowerUp.None)
                {
                    Log.DLazy(() => $"Power-up in slot {i}: {powerUp}", this);
                    _powerUps[i - 1].Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(_powerUpIcons.GetIcon(powerUp));
                    _powerUps[i - 1].style.display = DisplayStyle.Flex;
                }
                else
                {
                    Log.DLazy(() => $"No power-up in slot {i}", this);
                    _powerUps[i - 1].style.display = DisplayStyle.None;
                }
            }

            SelectPowerUp(inventory.SelectedSlot.Value.SlotIndex - 1);
        }

        private void InitPowerUpBar()
        {
            for (int i = 0; i < _powerUps.Length; i++)
            {
                _powerUps[i].style.display = DisplayStyle.None;
            }
        }
    }
}
