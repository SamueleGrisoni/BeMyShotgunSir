using BeMyShotgunSir.Scripts.Events.Abstracts;
using BeMyShotgunSir.Scripts.Core.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "SOAudioRequestEvent", menuName = "Be My Shotgun, Sir!/Events/SOAudioRequestEvent")]
public class SOAudioRequestEvent : SOEventDoubleParam<AudioRequest, AudioSource> { }

