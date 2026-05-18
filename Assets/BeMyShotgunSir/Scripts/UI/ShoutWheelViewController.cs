using System.Collections;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class ShoutWheelViewController : RaceBindTarget
    {
        [SerializeField] private UIDocument _hudDocument;
        [SerializeField] private SOShoutWheelIcons _shoutWheelIcons;
        [SerializeField] private SOPowerUpIcons _powerUpIcons;

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

            _viewModel.OnInventoryChanged += UpdatePowerUpIcon;
        }

        #region Visual Elements
        private VisualElement _root;
        private TemplateContainer _shoutWheel;
        private Button _goLeft;
        private Button _goRight;
        private VisualElement _powerUp;
        private VisualElement _sorry;
        private VisualElement _badDriver;
        private VisualElement _fast;
        private VisualElement _nicePlay;
        private VisualElement _caution;
        #endregion

        #region private fields
        private bool _isShowing = false;
        #endregion

        #region Public Fields
        public bool IsShowing => _isShowing;
        #endregion

        private void OnEnable()
        {
            if (_hudDocument == null)
            {
                Log.DLazy(() => "Shout Wheel Document reference missing!", this);
                return;
            }

            _root = _hudDocument.rootVisualElement;
            _shoutWheel = _root.Q<TemplateContainer>("ShoutWheel");
            _goLeft = _shoutWheel.Q<Button>("GoLeft");
            _goRight = _shoutWheel.Q<Button>("GoRight");
            _powerUp = _shoutWheel.Q<VisualElement>("PowerUp");
            _sorry = _shoutWheel.Q<VisualElement>("Sorry");
            _badDriver = _shoutWheel.Q<VisualElement>("BadDriver");
            _fast = _shoutWheel.Q<VisualElement>("Fast");
            _nicePlay = _shoutWheel.Q<VisualElement>("NicePlay");
            _caution = _shoutWheel.Q<VisualElement>("Caution");

            StartCoroutine(InitNextFrame());

            AssignShoutWheelIcons();
            Show(false);
        }

        IEnumerator InitNextFrame()
        {
            yield return null;

            _goLeft.clicked += GoLeftHandler;
            _goRight.clicked += GoRightHandler;

            _powerUp.RegisterCallback<PointerDownEvent>(PowerUpHandler);
            _sorry.RegisterCallback<PointerDownEvent>(SorryHandler);
            _badDriver.RegisterCallback<PointerDownEvent>(BadDriverHandler);
            _fast.RegisterCallback<PointerDownEvent>(FastHandler);
            _nicePlay.RegisterCallback<PointerDownEvent>(NicePlayHandler);
            _caution.RegisterCallback<PointerDownEvent>(CautionHandler);
        }

        #region Handlers

        private void GoLeftHandler()
        {
            Log.DLazy(() => "Go Left button pressed. Sending GoLeft message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.GoLeft);
        }
        private void GoRightHandler()
        {
            Log.DLazy(() => "Go Right button pressed. Sending GoRight message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.GoRight);
        }
        private void PowerUpHandler(PointerDownEvent evt)
        {
            Log.DLazy(() => "Power Up button pressed. Sending PowerUp message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.PowerUp);
        }
        private void SorryHandler(PointerDownEvent evt)
        {
            Log.DLazy(() => "Sorry button pressed. Sending Sorry message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.Sorry);
        }
        private void BadDriverHandler(PointerDownEvent evt)
        {
            Log.DLazy(() => "Bad Driver button pressed. Sending BadDriver message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.BadDriver);
        }
        private void FastHandler(PointerDownEvent evt)
        {
            Log.DLazy(() => "Fast button pressed. Sending Fast message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.Fast);
        }
        private void NicePlayHandler(PointerDownEvent evt)
        {
            Log.DLazy(() => "Nice Play button pressed. Sending NicePlay message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.NicePlay);
        }
        private void CautionHandler(PointerDownEvent evt)
        {
            Log.DLazy(() => "Caution button pressed. Sending Caution message.", this);
            _inputPublisher.SendWheelMessage(WheelMessages.Caution);
        }

        #endregion

        private void AssignShoutWheelIcons()
        {
            _powerUp.style.backgroundImage = null;
            _powerUp.style.display = DisplayStyle.None;
            _goLeft.style.backgroundImage = new StyleBackground(_shoutWheelIcons.GetIcon(WheelMessages.GoLeft));
            _goRight.style.backgroundImage = new StyleBackground(_shoutWheelIcons.GetIcon(WheelMessages.GoRight));
            _sorry.style.backgroundImage = new StyleBackground(_shoutWheelIcons.GetIcon(WheelMessages.Sorry));
            _badDriver.style.backgroundImage = new StyleBackground(_shoutWheelIcons.GetIcon(WheelMessages.BadDriver));
            _fast.style.backgroundImage = new StyleBackground(_shoutWheelIcons.GetIcon(WheelMessages.Fast));
            _nicePlay.style.backgroundImage = new StyleBackground(_shoutWheelIcons.GetIcon(WheelMessages.NicePlay));
            _caution.style.backgroundImage = new StyleBackground(_shoutWheelIcons.GetIcon(WheelMessages.Caution));
        }

        private void UpdatePowerUpIcon()
        {
            _viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId);
            if (teamId == null || !_viewModel.TeamInventories.TryGetValue(teamId.Value, out InventoryData inventory))
                return;

            if (inventory.SelectedSlot == null || inventory.SelectedSlot.Value.PowerUp == PowerUp.None)
            {
                _powerUp.style.backgroundImage = null;
                _powerUp.style.display = DisplayStyle.None;
                return;
            }

            Sprite icon = _powerUpIcons.GetIcon(inventory.SelectedSlot.Value.PowerUp);

            _powerUp.style.backgroundImage = new StyleBackground(icon);
            _powerUp.style.display = DisplayStyle.Flex;

        }



        public void Show(bool show)
        {
            _shoutWheel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _isShowing = show;
        }

    }
}
