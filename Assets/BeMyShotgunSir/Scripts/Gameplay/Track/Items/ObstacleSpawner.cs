using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public class ObstacleSpawner : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                Gizmos.color = Color.blueViolet;
                Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
            }
        }
    }
}
