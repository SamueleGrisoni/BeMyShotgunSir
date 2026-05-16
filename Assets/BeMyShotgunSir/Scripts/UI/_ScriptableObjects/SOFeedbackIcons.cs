using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

[CreateAssetMenu(fileName = "SOFeedbackIcons", menuName = "Be My Shotgun, Sir!/UI/FeedbackIcons")]
public class SOFeedbackIcons : ScriptableObject
{
    [System.Serializable]
    public struct FeedbackIconData
    {
        public DriverFeedback Feedback;
        public Sprite Icon;
    }

    [SerializeField] private FeedbackIconData[] _feedbackIcons;

    public Sprite GetIcon(DriverFeedback feedback)
    {
        foreach (FeedbackIconData data in _feedbackIcons)
        {
            if (data.Feedback == feedback)
            {
                return data.Icon;
            }
        }
        Log.ELazy(() => $"Icon for DriverFeedback {feedback} not found. Returning null.", this);
        return null;
    }
}
