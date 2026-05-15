using BeMyShotgunSir.Scripts.Events.Abstracts;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Events
{
    [CreateAssetMenu(fileName = "SOLoadingRequestEvent", menuName = "Be My Shotgun, Sir!/Events/SOLoadingRequestEvent")]
    public class SOLoadingRequestEvent : SOEventSingleParam<bool> { }
}

