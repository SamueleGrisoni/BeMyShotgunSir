using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public class SidecarCollisions : MonoBehaviour
    {
        [SerializeField] private float _bumpForce = 20f;
        [SerializeField] private Rigidbody _sphere;
        [SerializeField] private IDriverController _driverController;


        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("SidecarCollider"))
            {
                Debug.Log($"Trigger detected with: {other.gameObject.name} | {other.gameObject.layer}");
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Default"))
            {
                Debug.Log($"Trigger detected with default: {other.gameObject} | {other.gameObject.layer}");
                //_driverController.HandleDefaultBounce(-transform.forward);

            }
        }

        /*
        void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Urto rilevato");
        }
        */
    }
}
