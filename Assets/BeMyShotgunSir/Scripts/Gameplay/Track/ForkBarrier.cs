using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class ForkBarrier : MonoBehaviour
    {
        [SerializeField] public MeshRenderer[] BarrierMeshRenderers;

        public void SetBarrierVisible(bool visible)
        {
            foreach (var meshRenderer in BarrierMeshRenderers)
            {
                meshRenderer.enabled = visible;
            }
        }
    }
}
