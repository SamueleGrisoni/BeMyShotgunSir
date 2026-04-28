using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public class ItemAreaGroup : MonoBehaviour
    {
        [field: SerializeField] public PolygonSpawnArea[] ItemSpawnPoints { get; private set; }
    }
}

