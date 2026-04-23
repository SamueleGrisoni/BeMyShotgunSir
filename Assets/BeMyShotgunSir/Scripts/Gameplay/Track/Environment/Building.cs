using UnityEngine;


namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class Building : MonoBehaviour
    {
        public Renderer MeshRenderer { get; private set; }

        void Awake()
        {
            MeshRenderer = GetComponent<Renderer>();
        }

        void OnDrawGizmos()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
            }
        }

        public Bounds GetFlatBounds()
        {
            Renderer r = GetComponent<Renderer>();
            if (r == null) return new Bounds();
            Bounds b = r.bounds;
            b.center = new Vector3(b.center.x, 0f, b.center.z);
            b.size   = new Vector3(b.size.x,   0f, b.size.z);
            return b;
        }
    }
}

