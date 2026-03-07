using UnityEngine;

public class RoadChunk : MonoBehaviour
{
    public Renderer MeshRenderer { get; private set; }
    public float Length => _length;

    private float _length;

    void Awake()
    {
        MeshRenderer = GetComponent<Renderer>();
        _length = GetComponent<Renderer>().bounds.size.z;
    }

    void OnDrawGizmos()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
    }
}
