using UnityEngine;

public abstract class SceneInitializer : MonoBehaviour
{
    private void Start() => Initializer();
    protected abstract void Initializer();
}
