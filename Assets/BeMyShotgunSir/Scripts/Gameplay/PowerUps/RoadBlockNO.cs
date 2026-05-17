using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public class RoadBlockNO : NetworkBehaviour
    {
        [SerializeField] private float _lifeTime = 30f;
        [SerializeField] private float _blinkingDuration = 5f;
        [SerializeField] private float _blinkingFrequency = 0.15f;
        [SerializeField] private List<MeshRenderer> _renderers;
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            StartCoroutine(BlinkingCoroutine());
            if (IsServerStarted)
            {
                StartCoroutine(LifeTimeCoroutine());
            }
        }

        private IEnumerator LifeTimeCoroutine()
        {
            yield return new WaitForSeconds(_lifeTime);
            Despawn();
        }

        private IEnumerator BlinkingCoroutine()
        {
            yield return new WaitForSeconds(_lifeTime - _blinkingDuration);
            while (true)
            {
                foreach (MeshRenderer r in _renderers)
                    r.enabled = !r.enabled;
                yield return new WaitForSeconds(_blinkingFrequency);
            }
        }
    }
}
