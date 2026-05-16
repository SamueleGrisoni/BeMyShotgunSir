using System.Collections;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class CountdownViewController : RaceBindTarget
    {
        [SerializeField] private UIDocument _hudDriverDocument;
        [SerializeField] private float _startVisibleTime = 1f;
        [SerializeField] private float _fadeOutTime = 0.35f;

        #region Bindings
        private RaceCommand _command;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
        private IInputPublisher _inputPublisher;
        private RaceRole _role;
        #endregion

        private VisualElement _root;
        private VisualElement _countdownOverlay;
        private VisualElement _countdownPanel;
        private Label _countdownLabel;

        private Coroutine _countdownCoroutine;

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

            _viewModel.OnShowCountdown += ShowCountdownHandler;
        }

        private void OnEnable()
        {
            if (_hudDriverDocument == null)
            {
                Log.ELazy(() => "HUD Driver Document reference missing!", this);
                return;
            }

            _root = _hudDriverDocument.rootVisualElement;
            _countdownOverlay = _root.Q<VisualElement>("CountdownOverlay");
            _countdownPanel = _root.Q<VisualElement>("CountdownPanel");
            _countdownLabel = _root.Q<Label>("CountdownLabel");

            // HideImmediate();
            ShowCountdownHandler();
        }

        private void OnDisable()
        {
            if (_viewModel != null)
                _viewModel.OnShowCountdown -= ShowCountdownHandler;

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        private void ShowCountdownHandler()
        {
            if (_countdownCoroutine != null)
                StopCoroutine(_countdownCoroutine);

            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            int countdownTime = Mathf.CeilToInt(BMMSDefaults.COUNTDOWN_TIME);

            ShowImmediate();

            for (int i = countdownTime; i > 0; i--)
            {
                SetCountdownText(i.ToString());
                yield return new WaitForSecondsRealtime(1f);
            }

            SetCountdownText("START");
            yield return new WaitForSecondsRealtime(_startVisibleTime);

            _countdownOverlay.AddToClassList("countdown-overlay--fade-out");

            yield return new WaitForSecondsRealtime(_fadeOutTime);

            HideImmediate();
            _countdownCoroutine = null;
        }

        private void SetCountdownText(string text)
        {
            if (_countdownLabel == null || _countdownPanel == null)
                return;

            _countdownLabel.text = text;

            if (text == "START")
                _countdownLabel.AddToClassList("countdown-label--start");
            else
                _countdownLabel.RemoveFromClassList("countdown-label--start");

            _countdownPanel.RemoveFromClassList("countdown-panel--pulse");

            _countdownPanel.schedule.Execute(() =>
            {
                _countdownPanel.AddToClassList("countdown-panel--pulse");
            }).ExecuteLater(1);
        }

        private void ShowImmediate()
        {
            _root.style.display = DisplayStyle.Flex;

            _countdownOverlay.RemoveFromClassList("countdown-overlay--hidden");
            _countdownOverlay.RemoveFromClassList("countdown-overlay--fade-out");

            _countdownOverlay.style.opacity = 1f;
        }

        private void HideImmediate()
        {
            if (_root == null)
                return;

            _root.style.display = DisplayStyle.None;

            _countdownOverlay.RemoveFromClassList("countdown-overlay--fade-out");
            _countdownOverlay.AddToClassList("countdown-overlay--hidden");

            _countdownLabel.RemoveFromClassList("countdown-label--start");
        }
    }
}
