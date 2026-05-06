using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;


namespace BeMyShotgunSir.Scripts.UI
{
    public class BottomBarViewController : MonoBehaviour
    {
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
        #endregion

        #region Public Properties
        public float SteerValue { get; private set; }  // -1 → +1
        public float DriftValue { get; private set; }  // -1 → +1
        public bool IsDrifting { get; private set; }
        public float BoostValue { get; private set; }  // 0 → 1 (or more if overboost)
        #endregion


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

            // Set the correct picking modes for overlapping controls
            _feedbackContainer.pickingMode = PickingMode.Ignore;
            _boostControl.pickingMode = PickingMode.Ignore;

            StartCoroutine(InitNextFrame());
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
            _thumbDownButton.RegisterCallback<PointerDownEvent>(ThumbDownDownHandler);
            _thumbUpButton.RegisterCallback<PointerDownEvent>(ThumbUpDownHandler);

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
            // Debug.Log($"[Drift] PointerDown — pointerId={e.pointerId} pos={e.position}");
            if (_driftPointerId != -1) return;
            _driftPointerId = e.pointerId;
            _driftControl.CapturePointer(e.pointerId);

            _driftOriginX = e.localPosition.x;
            _driftMaxRadius = _driftControl.resolvedStyle.width * 0.25f;

            ShowDriftGhost(e.localPosition);
            DriftValue = 0f;
            e.StopPropagation();

            IsDrifting = true;
        }

        private void DriftMoveHandler(PointerMoveEvent e)
        {
            // Debug.Log($"[Drift] PointerMove — pos={e.position}");
            if (e.pointerId != _driftPointerId) return;

            float delta = Mathf.Clamp(e.localPosition.x - _driftOriginX, -_driftMaxRadius, _driftMaxRadius);
            DriftValue = delta / _driftMaxRadius;

            float joystickHalf = _driftJoystick.resolvedStyle.width * 0.5f;
            _driftJoystick.style.left = _driftOriginX + delta - joystickHalf;

            e.StopPropagation();
        }
        private void DriftUpHandler(PointerUpEvent e)
        {
            IsDrifting = false;
            ResetDrift(e.pointerId);
        }
        private void DriftCancelHandler(PointerCancelEvent e)
        {
            IsDrifting = false;
            ResetDrift(e.pointerId);
        }
        private void SteerDownHandler(PointerDownEvent e)
        {
            // Debug.Log($"[Steer] PointerDown — pointerId={e.pointerId} pos={e.position}");
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
            // Debug.Log($"[Steer] PointerMove — pos={e.position}");
            if (e.pointerId != _steerPointerId) return;
            ApplySteer(e.position.x);
            e.StopPropagation();
        }

        private void SteerUpHandler(PointerUpEvent e) => ResetSteer(e.pointerId);
        private void SteerCancelHandler(PointerCancelEvent e) => ResetSteer(e.pointerId);

        private void BoostDownHandler(PointerDownEvent e)
        {
            Debug.Log("[Boost] Pressed");
            // TODO: Boost command
        }

        private void ThumbDownDownHandler(PointerDownEvent e)
        {
            Debug.Log("[Feedback] Thumbs Down");
            // TODO: Feedback command
        }

        private void ThumbUpDownHandler(PointerDownEvent e)
        {
            Debug.Log("[Feedback] Thumbs Up");
            // TODO: Feedback command
        }

        #endregion


        private void ApplySteer(float screenX)
        {
            float delta = Mathf.Clamp(screenX - _steerCenterX, -_steerMaxRadius, _steerMaxRadius);
            SteerValue = delta / _steerMaxRadius;

            _steerJoystick.style.translate = new StyleTranslate(
                new Translate(new Length(delta, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel))
            );
        }

        private void ResetSteer(int pointerId)
        {
            if (pointerId != _steerPointerId) return;
            _steerControl.ReleasePointer(_steerPointerId);
            _steerPointerId = -1;
            SteerValue = 0f;
            _steerJoystick.style.translate = new StyleTranslate(
                new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel))
            );
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
            if (pointerId != _driftPointerId) return;
            _driftControl.ReleasePointer(_driftPointerId);
            _driftPointerId = -1;
            DriftValue = 0f;
            _driftOrigin.style.display = DisplayStyle.None;
            _driftJoystick.style.display = DisplayStyle.None;
        }

    }
}
