using System.Collections;
using BeMyShotgunSir.Scripts.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class HostOrJoinViewController : MonoBehaviour
    {
        [SerializeField] private UIControllerInit _uiControllerInit;
        [SerializeField] private UIDocument _hostOrJoinDocument;


        #region UI Elements
        private VisualElement _root;
        private VisualElement _hostJoinButtonContainer;
        private Button _hostButton;
        private Button _joinButton;
        private TextField _joinIPAddress;
        private Button _backButton;
        #endregion

        private void OnEnable()
        {
            if (_hostOrJoinDocument == null) return;
            _root = _hostOrJoinDocument.rootVisualElement;
            StartCoroutine(InitNextFrame());
            Show(false);
        }

        private IEnumerator InitNextFrame()
        {
            _hostJoinButtonContainer = _root.Q<VisualElement>("HostJoinButtonContainer");
            _hostButton = _root.Q<Button>("HostButton");
            _joinButton = _root.Q<Button>("JoinButton");
            _joinIPAddress = _root.Q<TextField>("JoinIPAddress");

            _backButton = _root.Q<Button>("BackButton");

            yield return null;

            _hostButton.clicked += HostButtonHandler;
            _joinButton.clicked += JoinButtonHandler;
            _joinIPAddress.RegisterValueChangedCallback(IpAddressChanged);

            _backButton.clicked += BackButtonHandler;

            _joinIPAddress.value = "localhost";
        }

        private void HostButtonHandler() => GameServices.Instance.ConnectionManager.StartHost(_joinIPAddress.value);

        private void JoinButtonHandler() => GameServices.Instance.ConnectionManager.StartJoin(_joinIPAddress.value);

        private void IpAddressChanged(ChangeEvent<string> evt) => _joinIPAddress.value = evt.newValue;

        private void BackButtonHandler() => _uiControllerInit.ShowScreen(UIScreen.CentralHub, true);

        private void OnDisable()
        {
            _hostButton.clicked -= HostButtonHandler;
            _joinButton.clicked -= JoinButtonHandler;
            _joinIPAddress.UnregisterValueChangedCallback(IpAddressChanged);

            _backButton.clicked -= BackButtonHandler;
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

