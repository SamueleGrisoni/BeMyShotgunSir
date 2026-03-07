using UnityEngine;
using UnityEngine.Pool;

public abstract class PooledObject<T, TH> : MonoBehaviour where T : Component where TH : Component
{
    private TH _component = null;
    public TH Component
    {
        get
        {
            if (_component == null)
                _component = GetComponent<TH>();
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
