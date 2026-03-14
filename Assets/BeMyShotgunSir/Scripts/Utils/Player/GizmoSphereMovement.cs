using UnityEngine;

namespace BeMyShotgunSir.Scripts.Utils.Player
{
    public class SidecarGizmo : MonoBehaviour
    {
        [SerializeField]
        private Transform _sidecar;
        [SerializeField]
        private Rigidbody _sphereRb;
        [SerializeField]
        private Transform _visualModel;
        [SerializeField]
        private float _visualScale = 5f;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            //Gizmos.DrawRay(transform.position, _sphereRb.linearVelocity * _visualScale);
            Gizmos.color = Color.red;
            //Gizmos.DrawRay(_sphereRb.transform.position, _sphereRb.transform.forward);
            Gizmos.color = Color.violet;
            Gizmos.DrawRay(_sidecar.position, _sidecar.forward * _visualScale);

            Gizmos.color = Color.black;
            Gizmos.DrawRay(_sphereRb.transform.position, _sphereRb.linearVelocity * _visualScale);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_visualModel.position, _visualModel.forward * _visualScale);
        }
    }

}