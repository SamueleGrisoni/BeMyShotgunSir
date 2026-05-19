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
    public class ShoutBoxViewController : RaceBindTarget
    {
        private bool _log = true;
        [SerializeField] private UIDocument _hudDocument;
        [SerializeField] private SOShoutWheelIcons _shoutWheelIcons;
        [SerializeField] private SOPowerUpIcons _powerUpIcons;

        [Header("Timing")]
        [SerializeField] private float _visibleTime = 1.5f;
        [SerializeField] private float _outTransitionTime = 0.25f;

        private RaceCommand _command;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
        private IInputPublisher _inputPublisher;
        private RaceRole _role;

        private VisualElement _root;
        private TemplateContainer _shoutBox;
        private VisualElement _shoutElement;

        private Coroutine _hideRoutine;

        public override void OnInitialBindComplete()
        {
            if (_initialBindSource == null)
            {
                Log.ELazy(() => "Initial bind source is null. Cannot complete initial bind.", this, _log);
                return;
            }

            _command = _initialBindSource.Command;
            _viewModel = _initialBindSource.ViewModel;
        }

        public override void OnFinalBindComplete()
        {
            if (_finalBindSource == null)
            {
                Log.ELazy(() => "Final bind source is null. Cannot complete final bind.", this, _log);
                return;
            }

            _roadManager = _finalBindSource.RoadManager;
            _inputPublisher = _finalBindSource.InputPublisher;
            _role = _finalBindSource.Role;

            if (_role == RaceRole.Driver)
                _inputPublisher.OnWheelMessageOnDriver += ShowShoutBox;
        }

        private void OnEnable()
        {
            _root = _hudDocument.rootVisualElement;
            _shoutBox = _root.Q<TemplateContainer>("ShoutBox");
            _shoutElement = _shoutBox?.Q<VisualElement>("ShoutElement");

            if (_shoutBox != null)
                HideImmediate();
        }

        private void OnDisable()
        {
            if (_role == RaceRole.Driver)
                _inputPublisher.OnWheelMessageOnDriver -= ShowShoutBox;

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }
        }

        private void ShowShoutBox(WheelMessages message)
        {
            if (_shoutBox == null || _shoutElement == null)
                return;

            if (message == WheelMessages.None)
            {
                HideImmediate();
                return;
            }

            Sprite icon = GetMessageIcon(message);

            if (icon == null)
            {
                Log.ELazy(() => $"No icon found for message {message}. Shout box will not be shown.", this, _log);
                return;
            }

            ShowIcon(icon);
        }

        private Sprite GetMessageIcon(WheelMessages message)
        {
            if (message != WheelMessages.PowerUp)
                return _shoutWheelIcons.GetIcon(message);

            if (!_viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId))
                return null;

            if (teamId == null || !_viewModel.TeamInventories.TryGetValue(teamId.Value, out InventoryData inventory))
                return null;

            if (inventory.SelectedSlot == null || inventory.SelectedSlot.Value.PowerUp == PowerUp.None)
                return null;

            return _powerUpIcons.GetIcon(inventory.SelectedSlot.Value.PowerUp);
        }

        private void ShowIcon(Sprite icon)
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            _shoutElement.style.backgroundImage = new StyleBackground(icon);

            _shoutBox.style.display = DisplayStyle.Flex;
            _shoutBox.RemoveFromClassList("shoutbox-out");
            _shoutBox.AddToClassList("shoutbox-in");

            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_visibleTime);

            _shoutBox.RemoveFromClassList("shoutbox-in");
            _shoutBox.AddToClassList("shoutbox-out");

            yield return new WaitForSeconds(_outTransitionTime);

            _shoutBox.style.display = DisplayStyle.None;
            _hideRoutine = null;
        }

        private void HideImmediate()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            _shoutBox.RemoveFromClassList("shoutbox-in");
            _shoutBox.AddToClassList("shoutbox-out");
            _shoutBox.style.display = DisplayStyle.None;
        }
    }
}
