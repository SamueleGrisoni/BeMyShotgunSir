using UnityEngine;

[CreateAssetMenu(fileName = "_Channels_", menuName = "Be My Shotgun, Sir!/Events/_Channels_", order = 0)]
public class SOChannels : ScriptableObject
{
    [field: SerializeField] public SOAudioRequestEvent AudioRequestEvent { get; private set; }
}
