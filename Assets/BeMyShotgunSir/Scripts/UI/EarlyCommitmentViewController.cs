using System.Collections;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class EarlyCommitmentViewController : RaceBindTarget
    {
        private bool _log = false;
        [SerializeField] private UIDocument _hudDocument;

        #region Visual Elements
        private VisualElement _root;
        private VisualElement _earlyCommitmentContainer;
        private VisualElement _earlyCommitment;
        private VisualElement _contentArea;
        private VisualElement _earlyCommitmentLabel;
        private VisualElement _commitmentChoice;
        private VisualElement _arrowLeft;
        private VisualElement _arrowRight;
        private VisualElement _commitBarMaskLeft;
        private VisualElement _commitBarMaskRight;
        #endregion

        #region private fields
        private float _commitmentValue;
        private float _currentCommitmentFill;
        private bool _updateEarlyCommitment;
        private float _lastCommitmentValue = 0f;
        private bool _onFinishRoad = false;
        private bool _isShowing = false;
        #endregion

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

            _inputPublisher.OnBatteryChargeEarlyCommitment += ReadEarlyCommitmentValue;
            _inputPublisher.OnEarlyCommitmentExecuted += EarlyCommitmentExecutedHandler;
        }



        private void OnEnable()
        {
            _root = _hudDocument.rootVisualElement;
            _earlyCommitmentContainer = _root.Q<VisualElement>("EarlyCommitmentContainer");
            _earlyCommitment = _root.Q<TemplateContainer>("EarlyCommitment");
            _contentArea = _earlyCommitment.Q<VisualElement>("ContentArea");
            _earlyCommitmentLabel = _earlyCommitment.Q<VisualElement>("EarlyCommitmentLabel");
            _commitmentChoice = _earlyCommitment.Q<VisualElement>("CommitmentChoice");
            _arrowLeft = _commitmentChoice.Q<VisualElement>("ArrowLeft");
            _arrowRight = _commitmentChoice.Q<VisualElement>("ArrowRight");
            _commitBarMaskLeft = _arrowLeft.Q<VisualElement>("CommitBarMaskLeft");
            _commitBarMaskRight = _arrowRight.Q<VisualElement>("CommitBarMaskRight");

            StartCoroutine(InitNextFrame());

            _earlyCommitmentContainer.pickingMode = PickingMode.Ignore;
            _earlyCommitment.pickingMode = PickingMode.Ignore;
            _earlyCommitment.Query<VisualElement>().ForEach(el => el.pickingMode = PickingMode.Ignore);

            _arrowLeft.pickingMode = PickingMode.Position;
            _arrowRight.pickingMode = PickingMode.Position;

            _earlyCommitmentContainer.style.display = DisplayStyle.None;

            _onFinishRoad = false;

            Log.DLazy(() => $"Role: {_role}, HUD: {_hudDocument.name}, GameObject: {gameObject.name}", this, _log);
            Log.DLazy(() => $"HUD instance id: {_hudDocument.GetInstanceID()}", this, _log);
        }

        IEnumerator InitNextFrame()
        {
            yield return null;

            _arrowLeft.RegisterCallback<PointerDownEvent>(ArrowLeftHandler);
            _arrowRight.RegisterCallback<PointerDownEvent>(ArrowRightHandler);
        }

        private void Update() => UpdateEarlyCommitment();

        private void OnDisable()
        {
            _arrowLeft.UnregisterCallback<PointerDownEvent>(ArrowLeftHandler);
            _arrowRight.UnregisterCallback<PointerDownEvent>(ArrowRightHandler);
        }

        #region Handlers
        private void ArrowLeftHandler(PointerDownEvent evt) => _inputPublisher.PressEarlyCommitment(CommitmentDirection.Left);
        private void ArrowRightHandler(PointerDownEvent evt) => _inputPublisher.PressEarlyCommitment(CommitmentDirection.Right);
        private void EarlyCommitmentExecutedHandler(float value, CommitmentDirection direction)
        {
            _updateEarlyCommitment = false;
            _lastCommitmentValue = value;
            ShowEarlyCommitment(false);
        }

        #endregion


        private void ReadEarlyCommitmentValue(float commitmentValue)
        {
            if (_onFinishRoad)
                return;

            _commitmentValue = commitmentValue;

            if (commitmentValue >= _lastCommitmentValue)
            {
                _updateEarlyCommitment = true;
                ShowEarlyCommitment(true);
            }
        }

        private void UpdateEarlyCommitment()
        {
            if (!_updateEarlyCommitment) return;

            _earlyCommitmentContainer.style.display = DisplayStyle.Flex;

            float targetFill = Mathf.Clamp01(_commitmentValue / 100);

            _currentCommitmentFill = Mathf.Lerp(_currentCommitmentFill, targetFill, Time.deltaTime * 8f);

            _commitBarMaskLeft.style.height = Length.Percent(_currentCommitmentFill * 100);
            _commitBarMaskRight.style.height = Length.Percent(_currentCommitmentFill * 100);

        }

        private Coroutine _hideEarlyCommitmentLabelCoroutine;

        private void ShowEarlyCommitment(bool show)
        {
            bool risingEdge = show && !_isShowing;

            _earlyCommitmentContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (risingEdge)
            {
                _earlyCommitmentLabel.RemoveFromClassList("hide");

                if (_hideEarlyCommitmentLabelCoroutine != null)
                    StopCoroutine(_hideEarlyCommitmentLabelCoroutine);

                _hideEarlyCommitmentLabelCoroutine = StartCoroutine(HideEarlyCommitmentLabel());
            }

            if (!show)
            {
                _isShowing = false;

                if (_hideEarlyCommitmentLabelCoroutine != null)
                {
                    StopCoroutine(_hideEarlyCommitmentLabelCoroutine);
                    _hideEarlyCommitmentLabelCoroutine = null;
                }

                _earlyCommitmentLabel.RemoveFromClassList("hide");
                return;
            }

            _isShowing = true;
        }

        private IEnumerator HideEarlyCommitmentLabel()
        {
            yield return new WaitForSeconds(2f); //DEBUG [UI] Here you can adjust how long the label stays visible after the commitment starts

            _earlyCommitmentLabel.AddToClassList("hide");
            _hideEarlyCommitmentLabelCoroutine = null;
        }


        public void OnFinishRoad() => _onFinishRoad = true;

    }
}
