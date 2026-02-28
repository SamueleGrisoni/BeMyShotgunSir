using UnityEngine;
using UnityEngine.Pool;

public abstract class PooledObject<T, H> : MonoBehaviour where T : Component where H : Component
{
    private H _component = null;
    public H Component
    {
        get
        {
            if (_component == null)
                _component = GetComponent<H>();
            return _component;
        }
        set => _component = value;
    }

    private IObjectPool<T> _myPool = null;
    public void SetPool(IObjectPool<T> pool) => _myPool = pool;

    public void ReturnToPool(float delay)
    {
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), delay);
    }
    public void ReturnToPool()
    {
        if (_myPool != null)
            _myPool.Release(this as T);
        else
            Destroy(gameObject);
    }

    public void OnDisable() => CancelInvoke();
}
