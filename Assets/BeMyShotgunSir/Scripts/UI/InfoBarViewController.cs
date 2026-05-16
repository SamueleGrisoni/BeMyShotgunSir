using System.Collections;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class InfoBarViewController : RaceBindTarget
    {
        [SerializeField] private UIDocument _hudDocument;
        [SerializeField] private SOFeedbackIcons _feedbackIcons;

        [Header("Timing")]
        [SerializeField] private float _visibleTime = 1.5f;
        [SerializeField] private float _outTransitionTime = 0.25f;

        #region Bindings
        private RaceCommand _command;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
        private IInputPublisher _inputPublisher;
        private RaceRole _role;
        #endregion

        #region Visual Elements
        private VisualElement _root;

        private VisualElement _boostBar;
        private VisualElement _overboostBar;

        private VisualElement _infoMessage;

        private VisualElement _feedbackIndicator;
        private VisualElement _feedbackIcon;

        private VisualElement _earlyCommitmentIndicator;
        private VisualElement _arrow;
        private VisualElement _commitBarMask;
        private VisualElement _commitBar;
        #endregion

        #region Private Fields
        private Coroutine _hideRoutine;
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

            _inputPublisher.OnDriverFeedbackToShotgun += ShowFeedback;
            _inputPublisher.OnEarlyCommitmentExecuted += ShowEarlyCommitment;
        }

        private void OnEnable()
        {
            _root = _hudDocument.rootVisualElement;

            _boostBar = _root.Q<VisualElement>("BoostBar");
            _overboostBar = _root.Q<VisualElement>("OverboostBar");

            _infoMessage = _root.Q<VisualElement>("InfoMessage");

            _feedbackIndicator = _root.Q<VisualElement>("FeedbackIndicator");
            _feedbackIcon = _root.Q<VisualElement>("FeedbackIcon");

            _earlyCommitmentIndicator = _root.Q<VisualElement>("EarlyCommitmentIndicator");
            _arrow = _root.Q<VisualElement>("Arrow");
            _commitBarMask = _root.Q<VisualElement>("CommitBarMask");
            _commitBar = _root.Q<VisualElement>("CommitBar");

            HideImmediate();
        }

        private void OnDisable()
        {
            if (_inputPublisher != null)
            {
                _inputPublisher.OnDriverFeedbackToShotgun -= ShowFeedback;
                _inputPublisher.OnEarlyCommitmentExecuted -= ShowEarlyCommitment;
            }

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }
        }

        private void Update()
        {
            if (_finalBindSource != null && _inputPublisher != null)
                UpdateBoostBar();
        }

        private void UpdateBoostBar()
        {
            if (_boostBar == null || _overboostBar == null)
                return;

            float boost = _inputPublisher.GetBatteryCharge();
            float boostFill = Mathf.Clamp01(boost / 100f);

            if (boost > 100f)
            {
                float overboost = boost - 100f;
                float overboostFill = Mathf.Clamp01(overboost / 100f);
                _overboostBar.style.height = Length.Percent(overboostFill * 100f);
            }
            else
            {
                _overboostBar.style.height = Length.Percent(0f);
            }

            _boostBar.style.height = Length.Percent(boostFill * 100f);
        }

        private void ShowFeedback(DriverFeedback feedback)
        {
            if (_infoMessage == null || _feedbackIndicator == null || _feedbackIcon == null)
                return;

            if (feedback == DriverFeedback.None)
            {
                HideImmediate();
                return;
            }

            Sprite icon = _feedbackIcons.GetIcon(feedback);

            if (icon == null)
            {
                Log.ELazy(() => $"No icon found for DriverFeedback {feedback}.", this);
                return;
            }

            _feedbackIcon.style.backgroundImage = new StyleBackground(icon);

            _feedbackIndicator.style.display = DisplayStyle.Flex;

            if (_earlyCommitmentIndicator != null)
                _earlyCommitmentIndicator.style.display = DisplayStyle.None;

            if (_commitBarMask != null)
                _commitBarMask.style.height = Length.Percent(0f);

            ShowInfoMessage();
        }

        private void ShowEarlyCommitment(float value, CommitmentDirection direction)
        {
            if (_infoMessage == null ||
                _earlyCommitmentIndicator == null ||
                _arrow == null ||
                _commitBar == null ||
                _commitBarMask == null)
                return;

            if (value <= 0f)
            {
                HideImmediate();
                return;
            }

            if (_feedbackIndicator != null)
                _feedbackIndicator.style.display = DisplayStyle.None;

            _earlyCommitmentIndicator.style.display = DisplayStyle.Flex;

            _arrow.RemoveFromClassList("left-arrow");
            _arrow.RemoveFromClassList("right-arrow");

            _commitBar.RemoveFromClassList("gauge-bar-left");
            _commitBar.RemoveFromClassList("gauge-bar-right");

            if (direction == CommitmentDirection.Left)
            {
                _arrow.AddToClassList("left-arrow");
                _commitBar.AddToClassList("gauge-bar-left");
            }
            else
            {
                _arrow.AddToClassList("right-arrow");
                _commitBar.AddToClassList("gauge-bar-right");
            }

            float fill = Mathf.Clamp01(value / 100f);
            _commitBarMask.style.height = Length.Percent(fill * 100f);

            ShowInfoMessage();
        }

        private void ShowInfoMessage()
        {
            if (_infoMessage == null)
                return;

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            _infoMessage.style.display = DisplayStyle.Flex;

            _infoMessage.RemoveFromClassList("info-message-out");
            _infoMessage.AddToClassList("info-message-in");

            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_visibleTime);

            _infoMessage.RemoveFromClassList("info-message-in");
            _infoMessage.AddToClassList("info-message-out");

            yield return new WaitForSeconds(_outTransitionTime);

            _infoMessage.style.display = DisplayStyle.None;
            _hideRoutine = null;
        }

        private void HideImmediate()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            if (_infoMessage != null)
            {
                _infoMessage.RemoveFromClassList("info-message-in");
                _infoMessage.AddToClassList("info-message-out");
                _infoMessage.style.display = DisplayStyle.None;
            }

            if (_feedbackIndicator != null)
                _feedbackIndicator.style.display = DisplayStyle.None;

            if (_earlyCommitmentIndicator != null)
                _earlyCommitmentIndicator.style.display = DisplayStyle.None;

            if (_feedbackIcon != null)
                _feedbackIcon.style.backgroundImage = null;

            if (_commitBarMask != null)
                _commitBarMask.style.height = Length.Percent(0f);
        }
    }
}
