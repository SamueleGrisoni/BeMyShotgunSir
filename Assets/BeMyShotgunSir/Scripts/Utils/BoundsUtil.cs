using UnityEngine;

namespace BeMyShotgunSir.Scripts.Utils
{
    public static class BoundsUtil
    {
        public static Bounds GetFullBounds(GameObject parent)
        {
            Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(parent.transform.position, Vector3.zero);
            }
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            return combinedBounds;
        }
    }
}

