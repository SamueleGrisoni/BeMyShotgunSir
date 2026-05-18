using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using FMODUnity;
using FMOD.Studio;

namespace BeMyShotgunSir.Scripts.UI
{
    public class CentralHubViewController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIControllerInit _uiControllerInit;
        [SerializeField] private UIDocument _centralHubDocument;

        [Header("FMOD VCA Paths")]
        [SerializeField] private string _masterVcaPath = "vca:/Master";
        [SerializeField] private string _musicVcaPath = "vca:/Music";
        [SerializeField] private string _sfxVcaPath = "vca:/SFX";

        // Variabili interne per FMOD
        private VCA _masterVca;
        private VCA _musicVca;
        private VCA _sfxVca;

        private VisualElement _root;
        private Button _playButton;
        private Button _settingsButton;
        private Button _creditsButton;

        #region Settings
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

            // 1. Inizializza i collegamenti ai VCA di FMOD
            _masterVca = RuntimeManager.GetVCA(_masterVcaPath);
            _musicVca = RuntimeManager.GetVCA(_musicVcaPath);
            _sfxVca = RuntimeManager.GetVCA(_sfxVcaPath);

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

            // 2. Sincronizziamo gli slider con i volumi reali di FMOD all'avvio
            SetupSliders();

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
            _settingsContainer.style.display = DisplayStyle.Flex;
            Debug.Log("Settings Button Clicked");
        }

        private void OnCloseSettingsButtonClicked()
        {
            _settingsContainer.style.display = DisplayStyle.None;
            Debug.Log("Close Settings Button Clicked");
        }

        private void OnCreditsButtonClicked()
        {
            Debug.Log("Credits Button Clicked");
        }

        private void SetupSliders()
        {
            float currentVolume;

            // Recupera e imposta il Master
            if (_masterVca.isValid())
            {
                _masterVca.getVolume(out currentVolume);
                _masterSlider.value = currentVolume * 10f;
            }

            // Recupera e imposta la Musica
            if (_musicVca.isValid())
            {
                _musicVca.getVolume(out currentVolume);
                _musicSlider.value = currentVolume * 10f;
            }

            // Recupera e imposta gli SFX
            if (_sfxVca.isValid())
            {
                _sfxVca.getVolume(out currentVolume);
                _sfxSlider.value = currentVolume * 10f;
            }
        }

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            // FMOD si aspetta un valore da 0 a 1, quindi basta dividere per 10
            float normalizedValue = evt.newValue / 10f;

            if (_masterVca.isValid())
                _masterVca.setVolume(normalizedValue);
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            float normalizedValue = evt.newValue / 10f;

            if (_musicVca.isValid())
                _musicVca.setVolume(normalizedValue);
        }

        private void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            float normalizedValue = evt.newValue / 10f;

            if (_sfxVca.isValid())
                _sfxVca.setVolume(normalizedValue);
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
