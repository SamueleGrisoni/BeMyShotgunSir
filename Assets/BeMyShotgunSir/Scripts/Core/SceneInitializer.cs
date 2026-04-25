using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

public abstract class SceneInitializer : MonoBehaviour
{
    protected abstract void Initializer();
    protected bool IsSceneInitialized { get; private set; } = false;
    protected bool blockRootInitialize = false;
    protected void RootInitialize()
    {
        if (IsSceneInitialized || blockRootInitialize)
        {
            Log.ELazy(() => "SceneInitializer: Attempted to initialize scene multiple times. Ignoring subsequent initialization.", this);
            return;
        }
        Initializer();
        IsSceneInitialized = true;
    }
    private void Start() => RootInitialize();
}
