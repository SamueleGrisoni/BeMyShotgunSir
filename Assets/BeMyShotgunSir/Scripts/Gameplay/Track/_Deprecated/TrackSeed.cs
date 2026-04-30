using System;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Deprecated
{
    public class TrackSeed : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private string _stringSeed = "DefaultSeed";
        [SerializeField] private bool _useRandomSeed = false;

        public System.Random Rng { get; private set; }

        private void Awake()
        {
            InitSeed();
        }

        private void InitSeed()
        {
            if (_useRandomSeed)
            {
                _stringSeed = DateTime.Now.Ticks.ToString();
            }
            int seed = _stringSeed.GetHashCode();
            Rng = new System.Random(seed);

            Debug.Log($"Tracciato generato con Seed: {_stringSeed} (Hash: {seed})");
        }
    }
}
