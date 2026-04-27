using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class CentralHubController : MonoBehaviour
    {
        [SerializeField] private UIControllerInit _uiControllerInit;
        [SerializeField] private UIDocument _centralHubDocument;

        private VisualElement _root;
        private Button _playButton;
        private Button _settingsButton;
        private Button _feedbackButton;


        private void OnEnable()
        {
            _root = _centralHubDocument.rootVisualElement;
            _playButton = _root.Q<Button>("PlayButton");
            _settingsButton = _root.Q<Button>("SettingsButton");
            _feedbackButton = _root.Q<Button>("FeedbackButton");

            StartCoroutine(InitNextFrame());
            Show(false);
        }

        private IEnumerator InitNextFrame()
        {
            yield return null;
            _playButton.clicked += OnPlayButtonClicked;
            _settingsButton.clicked += OnSettingsButtonClicked;
            _feedbackButton.clicked += OnFeedbackButtonClicked;
        }


        private void OnPlayButtonClicked()
        {
            _uiControllerInit.ShowScreen(UIScreen.HostOrJoin, true);
            Debug.Log("Play Button Clicked");
        }

        private void OnSettingsButtonClicked()
        {
            // Open the settings menu
            Debug.Log("Settings Button Clicked");
        }

        private void OnFeedbackButtonClicked()
        {
            // Open the feedback form or page
            Debug.Log("Feedback Button Clicked");
        }


        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;



    }
}
