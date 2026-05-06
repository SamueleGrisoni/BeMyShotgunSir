using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public class PowerUpSpawnable : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField] private float _bobHeight = 0.2f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] GameObject _itemVisualsPrefabs;

        private float _startLocalY;
        private float _minY;

        void Start()
        {
            _startLocalY = _itemVisualsPrefabs.transform.localPosition.y;
            _minY = _startLocalY;
        }

        void Update()
        {
            _itemVisualsPrefabs.transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f, Space.World);
            Vector3 localPos = _itemVisualsPrefabs.transform.localPosition;
            localPos.y = Mathf.Max(_minY, _startLocalY + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
            _itemVisualsPrefabs.transform.localPosition = localPos;
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
