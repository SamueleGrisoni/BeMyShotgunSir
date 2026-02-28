using UnityEngine;

namespace BeMyShotgunSir.Player
{
    public class Driver : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _speed = 10f;

        private void Start() => transform.position = Vector3.zero; // Start at origin, RoadManager will move us to the spawn point
        private void Update() => transform.Translate(Vector3.forward * (_speed * Time.deltaTime));
    }
}
