using UnityEngine;
using UnityEngine.UIElements;

public class LoadingScreenViewController : MonoBehaviour
{
    [SerializeField] private UIDocument _loadingScreenDocument;

    [Header("Wave Settings")]

    [SerializeField] private float _moveAmplitude = 20f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _rotationAmplitude = 20f;

    private VisualElement _root;
    private VisualElement _loadingImage;
    private bool _isShowing = false;

    private void OnEnable()
    {
        _root = _loadingScreenDocument.rootVisualElement;
        _loadingImage = _root.Q<VisualElement>("LoadingImage");
        Show(false);
    }

    private void Update()
    {
        if (_loadingImage == null)
            return;

        if (_isShowing)
            AnimateLoadingImage();
    }

    private void AnimateLoadingImage()
    {
        float t = Time.time;
        float y = Mathf.Sin(t * _moveSpeed) * _moveAmplitude;
        float slope = Mathf.Cos(t * _moveSpeed);
        float angle = slope * _rotationAmplitude;

        _loadingImage.style.translate = new Translate(0, y);
        _loadingImage.style.rotate = new Rotate(angle);
    }

    public void Show(bool show)
    {
        _isShowing = show;
        _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
