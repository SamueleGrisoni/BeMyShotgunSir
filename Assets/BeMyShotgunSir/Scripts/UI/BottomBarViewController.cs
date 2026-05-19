using System.Collections;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;


namespace BeMyShotgunSir.Scripts.UI
{
    public class BottomBarViewController : RaceBindTarget
    {
        private bool _log = false;
        [SerializeField] private UIDocument _hudDocument;

        #region Visual Elements
        private VisualElement _root;
        private VisualElement _bottomBar;
        private VisualElement _steerControl;
        private VisualElement _steerJoystick;

        private VisualElement _driftControl;
        private VisualElement _driftOrigin;
        private VisualElement _driftJoystick;

        private VisualElement _boostControl;
        private VisualElement _boostBar;
        private VisualElement _overboostBar;
        private VisualElement _boostButton;

        private VisualElement _feedbackContainer;
        private VisualElement _thumbDownButton;
        private VisualElement _thumbUpButton;
        #endregion

        #region Debug
        private Button _runStopButton;
        private bool _runStopToggle = false;
        #endregion

        #region Private Fields
        private int _steerPointerId = -1;
        private float _steerCenterX;
        private float _steerMaxRadius;

        private int _driftPointerId = -1;
        private float _driftOriginX;
        private float _driftMaxRadius;
        private float _driftOriginHalfW;
        private float _driftOriginHalfH;
        private float _driftJoystickHalfW;
        private float _driftJoystickHalfH;
        private float _steerValue;  // -1 → +1
        private float _driftValue;  // -1 → +1

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

        }




        private void OnEnable()
        {
            _root = _hudDocument.rootVisualElement;
            _bottomBar = _root.Q<TemplateContainer>("BottomBar");
            _steerControl = _bottomBar.Q("SteerControl");
            _steerJoystick = _bottomBar.Q("SteerJoystick");
            _driftControl = _bottomBar.Q("DriftControl");
            _driftOrigin = _bottomBar.Q("DriftOrigin");
            _driftJoystick = _bottomBar.Q("DriftJoystick");
            _boostControl = _bottomBar.Q("BoostControl");
            _boostBar = _boostControl.Q("BoostBar");
            _overboostBar = _boostControl.Q("OverboostBar");
            _boostButton = _boostControl.Q("BoostButton");
            _feedbackContainer = _bottomBar.Q("FeedbackContainer");
            _thumbDownButton = _feedbackContainer.Q("ThumbDownButton");
            _thumbUpButton = _feedbackContainer.Q("ThumbUpButton");

            _runStopButton = _bottomBar.Q<Button>("RunStopButton");

            // Set the correct picking modes for overlapping controls
            _feedbackContainer.pickingMode = PickingMode.Ignore;
            _boostControl.pickingMode = PickingMode.Ignore;

            StartCoroutine(InitNextFrame());

            Log.DLazy(() => $"Role: {_role}, HUD: {_hudDocument.name}, GameObject: {gameObject.name}", this, _log);
            Log.DLazy(() => $"HUD instance id: {_hudDocument.GetInstanceID()}", this, _log);
        }

        private IEnumerator InitNextFrame()
        {
            _driftOrigin.style.display = DisplayStyle.Flex;
            _driftJoystick.style.display = DisplayStyle.Flex;

            yield return null;

            _steerControl.RegisterCallback<PointerDownEvent>(SteerDownHandler);
            _steerControl.RegisterCallback<PointerMoveEvent>(SteerMoveHandler);
            _steerControl.RegisterCallback<PointerUpEvent>(SteerUpHandler);
            _steerControl.RegisterCallback<PointerCancelEvent>(SteerCancelHandler);

            _driftOriginHalfW = _driftOrigin.resolvedStyle.width * 0.5f;
            _driftOriginHalfH = _driftOrigin.resolvedStyle.height * 0.5f;
            _driftJoystickHalfW = _driftJoystick.resolvedStyle.width * 0.5f;
            _driftJoystickHalfH = _driftJoystick.resolvedStyle.height * 0.5f;

            _driftOrigin.style.display = DisplayStyle.None;
            _driftJoystick.style.display = DisplayStyle.None;

            _driftControl.RegisterCallback<PointerDownEvent>(DriftDownHandler);
            _driftControl.RegisterCallback<PointerMoveEvent>(DriftMoveHandler);
            _driftControl.RegisterCallback<PointerUpEvent>(DriftUpHandler);
            _driftControl.RegisterCallback<PointerCancelEvent>(DriftCancelHandler);

            _boostButton.RegisterCallback<PointerDownEvent>(BoostDownHandler);
            _thumbDownButton.RegisterCallback<PointerDownEvent>(ThumbsDownDownHandler);
            _thumbUpButton.RegisterCallback<PointerDownEvent>(ThumbsUpDownHandler);

            _runStopButton.clicked += RunStopDownHandler;
        }

        private void Update()
        {
            if (_finalBindSource != null)
                UpdateBoostBar();
        }

        private void OnDisable()
        {
            _steerControl.UnregisterCallback<PointerDownEvent>(SteerDownHandler);
            _steerControl.UnregisterCallback<PointerMoveEvent>(SteerMoveHandler);
            _steerControl.UnregisterCallback<PointerUpEvent>(SteerUpHandler);
            _steerControl.UnregisterCallback<PointerCancelEvent>(SteerCancelHandler);

            _driftControl.UnregisterCallback<PointerDownEvent>(DriftDownHandler);
            _driftControl.UnregisterCallback<PointerMoveEvent>(DriftMoveHandler);
            _driftControl.UnregisterCallback<PointerUpEvent>(DriftUpHandler);
            _driftControl.UnregisterCallback<PointerCancelEvent>(DriftCancelHandler);
        }

        #region Handlers

        private void DriftDownHandler(PointerDownEvent e)
        {
            if (_driftPointerId != -1) return;
            _driftPointerId = e.pointerId;
            _driftControl.CapturePointer(e.pointerId);

            _driftOriginX = e.localPosition.x;
            _driftMaxRadius = _driftControl.resolvedStyle.width * 0.25f;

            // ShowDriftGhost(e.localPosition); // Optional: Show a visual indicator for the drift control
            _driftValue = 0f;
            e.StopPropagation();

            _inputPublisher.SetIsDrifting(true);
            _inputPublisher.SetDriftInput(_driftValue);
        }

        private void DriftMoveHandler(PointerMoveEvent e)
        {
            if (e.pointerId != _driftPointerId) return;

            float delta = Mathf.Clamp(e.localPosition.x - _driftOriginX, -_driftMaxRadius, _driftMaxRadius);
            _driftValue = delta / _driftMaxRadius;

            float joystickHalf = _driftJoystick.resolvedStyle.width * 0.5f;
            _driftJoystick.style.left = _driftOriginX + delta - joystickHalf;

            e.StopPropagation();
            _inputPublisher.SetDriftInput(_driftValue);

        }
        private void DriftUpHandler(PointerUpEvent e)
        {
            _inputPublisher.SetIsDrifting(false);
            _inputPublisher.SetDriftInput(0);
            ResetDrift(e.pointerId);
        }
        private void DriftCancelHandler(PointerCancelEvent e)
        {
            _inputPublisher.SetIsDrifting(false);
            _inputPublisher.SetDriftInput(0);
            ResetDrift(e.pointerId);
        }
        private void SteerDownHandler(PointerDownEvent e)
        {
            if (_steerPointerId != -1) return;
            _steerPointerId = e.pointerId;
            _steerControl.CapturePointer(e.pointerId);

            Rect wb = _steerJoystick.worldBound;
            _steerCenterX = wb.center.x;
            _steerMaxRadius = wb.width * 0.5f;

            ApplySteer(e.position.x);
            e.StopPropagation();
        }

        private void SteerMoveHandler(PointerMoveEvent e)
        {
            if (e.pointerId != _steerPointerId) return;
            ApplySteer(e.position.x);
            e.StopPropagation();
        }

        private void SteerUpHandler(PointerUpEvent e)
        {
            Log.DLazy(() => $"[Steer] PointerUp — pointerId={e.pointerId}", this, _log);
            ResetSteer(e.pointerId);
        }
        private void SteerCancelHandler(PointerCancelEvent e)
        {
            Log.DLazy(() => $"[Steer] PointerCancel — pointerId={e.pointerId}", this, _log);
            ResetSteer(e.pointerId);
        }

        private void BoostDownHandler(PointerDownEvent e)
        {
            Log.DLazy(() => $"[Boost] Pressed", this, _log);
            _inputPublisher.PressBoost();
        }

        private void ThumbsDownDownHandler(PointerDownEvent e)
        {
            Log.DLazy(() => $"[Feedback] Thumbs Down", this, _log);
            _inputPublisher.PressDriverFeedback(DriverFeedback.ThumbsDown);
        }

        private void ThumbsUpDownHandler(PointerDownEvent e)
        {
            Log.DLazy(() => $"[Feedback] Thumbs Up", this, _log);
            _inputPublisher.PressDriverFeedback(DriverFeedback.ThumbsUp);
        }

        private void RunStopDownHandler()
        {
            Log.DLazy(() => $"[Debug] Run/Stop Toggled", this, _log);
            _runStopToggle = !_runStopToggle;
            if (_runStopToggle) _runStopButton.AddToClassList("toggled");
            else _runStopButton.RemoveFromClassList("toggled");

            _inputPublisher.PressStart();
        }

        #endregion


        private void ApplySteer(float screenX)
        {
            float delta = Mathf.Clamp(screenX - _steerCenterX, -_steerMaxRadius, _steerMaxRadius);
            _steerValue = delta / _steerMaxRadius;

            _steerJoystick.style.translate = new StyleTranslate(
                new Translate(new Length(delta, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel))
            );

            _inputPublisher.SetSteerInput(_steerValue);
            Log.DLazy(() => $"[Steer] Value={_steerValue:F2}", this, _log);
        }

        private void ResetSteer(int pointerId)
        {
            if (pointerId != _steerPointerId) return;
            _steerControl.ReleasePointer(_steerPointerId);
            _steerPointerId = -1;
            _steerValue = 0f;
            _steerJoystick.style.translate = new StyleTranslate(
                new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel))
            );

            _inputPublisher.SetSteerInput(0);
        }


        private void ShowDriftGhost(Vector2 localPos)
        {
            _driftOrigin.style.display = DisplayStyle.Flex;
            _driftJoystick.style.display = DisplayStyle.Flex;

            _driftOrigin.style.left = localPos.x - _driftOriginHalfW;
            _driftOrigin.style.top = localPos.y - _driftOriginHalfH;

            _driftJoystick.style.left = localPos.x - _driftJoystickHalfW;
            _driftJoystick.style.top = localPos.y - _driftJoystickHalfH;
        }

        private void ResetDrift(int pointerId)
        {
            Log.DLazy(() => $"[Drift] Resetting drift — pointerId={pointerId}", this, _log);
            if (pointerId != _driftPointerId) return;
            _driftControl.ReleasePointer(_driftPointerId);
            _driftPointerId = -1;
            _driftValue = 0f;
            _driftOrigin.style.display = DisplayStyle.None;
            _driftJoystick.style.display = DisplayStyle.None;
        }

        private void UpdateBoostBar()
        {
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

    }
}
