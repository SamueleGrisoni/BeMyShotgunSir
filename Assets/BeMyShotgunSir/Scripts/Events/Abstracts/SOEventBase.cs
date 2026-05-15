using System;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Events.Abstracts
{
    //Name must match exactly with the SOEventBase asset names (without the "Event" suffix)
    public enum BMSSEventsEnum
    {
        None = 0,
        AudioRequest = 1,
        LoadingRequest = 2,
    }

    public abstract class SOEventBase : ScriptableObject
    {
        [field: SerializeField] public BMSSEventsEnum EventType { get; private set; } = BMSSEventsEnum.None;

        private void OnValidate()
        {
            string assetName = name;

            if (string.IsNullOrEmpty(assetName)) return;
            string cleanedName = assetName.Replace("Event", "").Trim();
            if (Enum.TryParse(cleanedName, true, out BMSSEventsEnum result))
            {
                if (EventType != result)
                {
                    EventType = result;
                    Log.TLazy(() => $"Event automatically assigned: {result} for asset {assetName}", this);
                }
            }
            else
                Log.ELazy(() => $"No matching Enum value found for name: {cleanedName}", this);
        }
    }
}
