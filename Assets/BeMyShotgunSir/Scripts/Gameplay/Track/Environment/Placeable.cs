using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class Placeable : MonoBehaviour
    {
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
            if (r == null)
            {
                Debug.LogError("Placeable: No renderer found. Cannot calculate bounds. " + name);
                return new Bounds();
            }
            Bounds b = r.bounds;
            b.center = new Vector3(b.center.x, 0f, b.center.z);
            b.size   = new Vector3(b.size.x,   0f, b.size.z);
            return b;
        }
    }
}
