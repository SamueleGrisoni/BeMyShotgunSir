using BeMyShotgunSir.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BeMyShotgunSir.Scripts.UI
{
    public class LobbyMenu : MonoBehaviour
    {
        [SerializeField] private Button _quitButton;

        public void Start()
        {
            GameServices.Instance.LobbyManager.UiMenu = this;
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        private void OnQuitButtonClicked() =>
         GameServices.Instance.ConnectionManager.ChangeState(InitManagerState.Init);
    }
}
