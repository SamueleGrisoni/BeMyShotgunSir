using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public enum ItemType { POWER_UP, OBSTACLE }

    public class Item : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Item " + gameObject.name + " collided with " + other.gameObject.name);
            Destroy(gameObject);
        }
        void OnDrawGizmos()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
            }
        }
    }
}
