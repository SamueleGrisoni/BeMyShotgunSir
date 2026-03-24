using BeMyShotgunSir.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeMyShotgunSir.Scripts.UI
{
    public class InitMenu : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private TMP_InputField _ipAddressField;
        [SerializeField] private Button _joinButton;
        private string _joinIP = "localhost";

        private void Awake()
        {
            int i = 0;
            _hostButton.onClick.AddListener(OnHostButtonClicked);
            _joinButton.onClick.AddListener(OnJoinButtonClicked);
            _ipAddressField.onValueChanged.AddListener(OnIPValueChanged);
        }

        private void OnHostButtonClicked() =>
         GameServices.Instance.ConnectionManager.StartHost();

        private void OnIPValueChanged(string newIP) =>
         _joinIP = newIP;

        private void OnJoinButtonClicked() =>
         GameServices.Instance.ConnectionManager.StartJoin(_joinIP);

    }
}
