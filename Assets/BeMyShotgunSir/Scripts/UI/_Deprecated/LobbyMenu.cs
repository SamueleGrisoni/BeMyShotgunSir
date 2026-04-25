using BeMyShotgunSir.Scripts.Core.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace BeMyShotgunSir.Scripts.UI
{
    public class LobbyMenu : LobbyBindTarget
    {
        [SerializeField] private Button _quitButton;
        public override void OnBindComplete() { }

        private void Awake() =>
            _quitButton.onClick.AddListener(OnQuitButtonClicked);

        private void OnDestroy() =>
            _quitButton.onClick.RemoveListener(OnQuitButtonClicked);

        private void OnQuitButtonClicked() =>
            _command.QuitLobby_CMRequest();
    }
}
