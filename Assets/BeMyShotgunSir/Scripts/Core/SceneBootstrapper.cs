using UnityEngine;

public abstract class SceneBootstrapper : MonoBehaviour
{
    protected abstract void Initialize();
    protected bool _hasInitialized { get; private set; } = false;
    /// <summary>
    /// When set to true, avoids automatic initialization <br/>
    /// Used in cases where the Initializer needs to be called manually after certain conditions are met
    /// </summary>
    protected bool _suppressAutoInitialize = false;
    protected void TryInitialize()
    {
        if (_hasInitialized || _suppressAutoInitialize)
            return;
        Initialize();
        _hasInitialized = true;
    }
    private void Start() => TryInitialize();
}
