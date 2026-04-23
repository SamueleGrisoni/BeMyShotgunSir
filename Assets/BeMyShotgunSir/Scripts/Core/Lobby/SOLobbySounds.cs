using BeMyShotgunSir.Scripts.Core.Audio;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    [CreateAssetMenu(fileName = "SOLobbySounds", menuName = "Be My Shotgun, Sir!/Audio/SOLobbySounds", order = 0)]
    public class SOLobbySounds : SOSounds
    {
        [field: SerializeField] public AudioClip JoinLobbyClip { get; private set; }
        [field: SerializeField] public AudioClip LeaveLobbyClip { get; private set; }
    }
}
