using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public class Driver : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;
        [FormerlySerializedAs("_fowardSpeed")]
        [SerializeField] private float _forwardSpeed = 10f;
        [SerializeField] private float _horizontalSpeed = 5f;

        private void Start() => transform.position = Vector3.zero; // Start at origin, RoadManager will move us to the spawn point
        private void Update()
        {
            transform.Translate(Vector3.forward * (_forwardSpeed * Time.deltaTime));

            float horizontalInput = 0;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed)
                    horizontalInput = -1f;
                else if (Keyboard.current.dKey.isPressed)
                    horizontalInput = 1f;
            }
            Vector3 movement = new(horizontalInput * _horizontalSpeed, 0, _forwardSpeed);
            transform.Translate(movement * Time.deltaTime, Space.World);
        }
    }
}
