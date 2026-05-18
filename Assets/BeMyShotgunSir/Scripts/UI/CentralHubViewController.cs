using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class CentralHubViewController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIControllerInit _uiControllerInit;
        [SerializeField] private UIDocument _centralHubDocument;

        [Header("Music Sliders")]
        [SerializeField] private AudioMixer _masterMixer;
        [SerializeField] private string[] _mixerGroupsVolumes = { "Master", "Soundtrack", "SFX" };

        private VisualElement _root;
        private Button _playButton;
        private Button _settingsButton;
        private Button _creditsButton;

        #region  Settings
        private VisualElement _settingsContainer;
        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private Button _closeSettingsButton;
        #endregion



        private void OnEnable()
        {
            _root = _centralHubDocument.rootVisualElement;
            _playButton = _root.Q<Button>("PlayButton");
            _settingsButton = _root.Q<Button>("SettingsButton");
            _creditsButton = _root.Q<Button>("CreditsButton");
            _settingsContainer = _root.Q<VisualElement>("SettingsContainer");
            _masterSlider = _root.Q<Slider>("MasterSlider");
            _musicSlider = _root.Q<Slider>("MusicSlider");
            _sfxSlider = _root.Q<Slider>("SfxSlider");
            _closeSettingsButton = _root.Q<Button>("CloseSettingsButton");

            _masterSlider.lowValue = 0f;
            _masterSlider.highValue = 10f;
            _musicSlider.lowValue = 0f;
            _musicSlider.highValue = 10f;
            _sfxSlider.lowValue = 0f;
            _sfxSlider.highValue = 10f;


            StartCoroutine(InitNextFrame());
            Show(false);
        }

        private IEnumerator InitNextFrame()
        {
            yield return null;
            _playButton.clicked += OnPlayButtonClicked;
            _settingsButton.clicked += OnSettingsButtonClicked;
            _creditsButton.clicked += OnCreditsButtonClicked;
            _closeSettingsButton.clicked += OnCloseSettingsButtonClicked;

            _masterSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
            _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
            _sfxSlider.RegisterValueChangedCallback(OnSfxVolumeChanged);
        }


        private void OnPlayButtonClicked()
        {
            _uiControllerInit.ShowScreen(UIScreen.HostOrJoin, true);
            Debug.Log("Play Button Clicked");
        }

        private void OnSettingsButtonClicked()
        {
            // Open the settings menu
            _settingsContainer.style.display = DisplayStyle.Flex;
            Debug.Log("Settings Button Clicked");
        }

        private void OnCloseSettingsButtonClicked()
        {
            // Close the settings menu
            _settingsContainer.style.display = DisplayStyle.None;
            Debug.Log("Close Settings Button Clicked");
        }

        private void OnCreditsButtonClicked()
        {
            // Open the credits page
            Debug.Log("Credits Button Clicked");
        }



        private void SetupSliders()
        {
            float currentDB;
            float linearValue;

            // MASTER
            if (_masterMixer.GetFloat(_mixerGroupsVolumes[0], out currentDB))
            {
                linearValue = Mathf.Pow(10f, currentDB / 20f);
                // Multiply by 10 to scale the 0-1 range to the 0-10 slider range
                _masterSlider.value = linearValue * 10f;
            }

            // MUSIC
            if (_masterMixer.GetFloat(_mixerGroupsVolumes[1], out currentDB))
            {
                linearValue = Mathf.Pow(10f, currentDB / 20f);
                _musicSlider.value = linearValue * 10f;
            }

            // SFX
            if (_masterMixer.GetFloat(_mixerGroupsVolumes[2], out currentDB))
            {
                linearValue = Mathf.Pow(10f, currentDB / 20f);
                _sfxSlider.value = linearValue * 10f;
            }
        }

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            // The received value (evt.newValue) is between 0 and 10
            float rawValue = evt.newValue;

            // Normalize: scale 0-10 to 0-1.
            float normalizedValue = rawValue / 10f;

            // Convert to Decibels (dB) using the normalized value.
            // Using 0.0001f prevents issues with log(0).
            float dBValue = Mathf.Log10(Mathf.Clamp(normalizedValue, 0.0001f, 1f)) * 20f;

            _masterMixer.SetFloat(_mixerGroupsVolumes[0], dBValue);
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            float rawValue = evt.newValue;
            float normalizedValue = rawValue / 10f;

            float dBValue = Mathf.Log10(Mathf.Clamp(normalizedValue, 0.0001f, 1f)) * 20f;
            _masterMixer.SetFloat(_mixerGroupsVolumes[1], dBValue);
        }

        private void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            float rawValue = evt.newValue;
            float normalizedValue = rawValue / 10f;

            float dBValue = Mathf.Log10(Mathf.Clamp(normalizedValue, 0.0001f, 1f)) * 20f;
            _masterMixer.SetFloat(_mixerGroupsVolumes[2], dBValue);
        }



        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

    }
}
