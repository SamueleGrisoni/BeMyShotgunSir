using UnityEngine;


namespace BeMyShotgunSir.Scripts.Utils
{
    public class Building : MonoBehaviour
    {
        public int ID { get; set; }

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
    }
}

