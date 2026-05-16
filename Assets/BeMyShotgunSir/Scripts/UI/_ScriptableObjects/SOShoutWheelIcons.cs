using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

[CreateAssetMenu(fileName = "SOShoutWheelIcons", menuName = "Be My Shotgun, Sir!/UI/ShoutWheelIcons")]
public class SOShoutWheelIcons : ScriptableObject
{
    [System.Serializable]
    public struct ShoutWheelIconData
    {
        public WheelMessages Message;
        public Sprite Icon;
    }

    [SerializeField] private ShoutWheelIconData[] _shoutWheelIcons;

    public Sprite GetIcon(WheelMessages message)
    {
        foreach (ShoutWheelIconData data in _shoutWheelIcons)
        {
            if (data.Message == message)
            {
                return data.Icon;
            }
        }
        Log.ELazy(() => $"Icon for WheelMessage {message} not found. Returning null.", this);
        return null;
    }
}
