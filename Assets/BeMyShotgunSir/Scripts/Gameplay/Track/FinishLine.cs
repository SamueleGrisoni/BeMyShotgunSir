using System;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class FinishLine : MonoBehaviour
    {
        public static Action OnFinishLineCrossed;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnFinishLineCrossed?.Invoke();
            }
        }
    }
}
