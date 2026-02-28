using UnityEngine;

public class RoadChunk : MonoBehaviour
{
    public Renderer MeshRenderer { get; private set; }
    public float Length => length;

    private float length;

    void Awake()
    {
        MeshRenderer = GetComponent<Renderer>();
        length = GetComponent<Renderer>().bounds.size.z;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
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
