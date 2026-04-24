using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class BottomBarController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;

    private VisualElement _root;
    private VisualElement _bottomBar;

    public float SteerValue { get; private set; }  // -1 → +1
    public float DriftValue { get; private set; }  // -1 → +1

    private VisualElement _steerControl;
    private VisualElement _steerJoystick;

    private VisualElement _driftControl;
    private VisualElement _driftOrigin;
    private VisualElement _driftJoystick;

    private int _steerPointerId = -1;
    private float _steerCenterX;
    private float _steerMaxRadius;

    private int _driftPointerId = -1;
    private float _driftOriginX;
    private float _driftMaxRadius;

    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        _root = _hudDocument.rootVisualElement;
        _bottomBar = _root.Q<TemplateContainer>("BottomBar");
        _bottomBar.pickingMode = PickingMode.Position; // Let events pass through empty areas
        _steerControl = _bottomBar.Q("SteerControl");
        _steerJoystick = _bottomBar.Q("SteerJoystick");
        _driftControl = _bottomBar.Q("DriftControl");
        _driftOrigin = _bottomBar.Q("DriftOrigin");
        _driftJoystick = _bottomBar.Q("DriftJoystick");

        StartCoroutine(InitNextFrame());
    }

    IEnumerator InitNextFrame()
    {
        yield return null;

        _steerControl.RegisterCallback<PointerDownEvent>(OnSteerDown);
        _steerControl.RegisterCallback<PointerMoveEvent>(OnSteerMove);
        _steerControl.RegisterCallback<PointerUpEvent>(OnSteerUp);
        _steerControl.RegisterCallback<PointerCancelEvent>(OnSteerCancel);

        _driftControl.RegisterCallback<PointerDownEvent>(OnDriftDown);
        _driftControl.RegisterCallback<PointerMoveEvent>(OnDriftMove);
        _driftControl.RegisterCallback<PointerUpEvent>(OnDriftUp);
        _driftControl.RegisterCallback<PointerCancelEvent>(OnDriftCancel);
    }

    private void OnDisable()
    {
        _steerControl.UnregisterCallback<PointerDownEvent>(OnSteerDown);
        _steerControl.UnregisterCallback<PointerMoveEvent>(OnSteerMove);
        _steerControl.UnregisterCallback<PointerUpEvent>(OnSteerUp);
        _steerControl.UnregisterCallback<PointerCancelEvent>(OnSteerCancel);

        _driftControl.UnregisterCallback<PointerDownEvent>(OnDriftDown);
        _driftControl.UnregisterCallback<PointerMoveEvent>(OnDriftMove);
        _driftControl.UnregisterCallback<PointerUpEvent>(OnDriftUp);
        _driftControl.UnregisterCallback<PointerCancelEvent>(OnDriftCancel);
    }


    private void OnSteerDown(PointerDownEvent e)
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

    private void OnSteerMove(PointerMoveEvent e)
    {
        // Debug.Log($"[Steer] PointerMove — pos={e.position}");
        if (e.pointerId != _steerPointerId) return;
        ApplySteer(e.position.x);
        e.StopPropagation();
    }

    private void OnSteerUp(PointerUpEvent e) => ResetSteer(e.pointerId);
    private void OnSteerCancel(PointerCancelEvent e) => ResetSteer(e.pointerId);

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


    private void OnDriftDown(PointerDownEvent e)
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
    }

    private void OnDriftMove(PointerMoveEvent e)
    {
        // Debug.Log($"[Drift] PointerMove — pos={e.position}");
        if (e.pointerId != _driftPointerId) return;

        float delta = Mathf.Clamp(e.localPosition.x - _driftOriginX, -_driftMaxRadius, _driftMaxRadius);
        DriftValue = delta / _driftMaxRadius;

        float joystickHalf = _driftJoystick.resolvedStyle.width * 0.5f;
        _driftJoystick.style.left = _driftOriginX + delta - joystickHalf;

        e.StopPropagation();
    }

    private void OnDriftUp(PointerUpEvent e) => ResetDrift(e.pointerId);
    private void OnDriftCancel(PointerCancelEvent e) => ResetDrift(e.pointerId);

    private void ShowDriftGhost(Vector2 localPos)
    {
        float originHalfX = _driftOrigin.resolvedStyle.width * 0.5f;
        float originHalfY = _driftOrigin.resolvedStyle.height * 0.5f;
        float joystickHalfX = _driftJoystick.resolvedStyle.width * 0.5f;
        float joystickHalfY = _driftJoystick.resolvedStyle.height * 0.5f;


        _driftOrigin.style.display = DisplayStyle.Flex;
        _driftJoystick.style.display = DisplayStyle.Flex;

        _driftOrigin.style.left = localPos.x - originHalfX;
        _driftOrigin.style.top = localPos.y - originHalfY;

        _driftJoystick.style.left = localPos.x - joystickHalfX;
        _driftJoystick.style.top = localPos.y - joystickHalfY;
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
