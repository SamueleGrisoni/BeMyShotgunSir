using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    public class UIController_Lobby : MonoBehaviour
    {
        [Header("UI Controllers")]
        [Header("Lobby Menu")]
        [SerializeField] private LobbyViewController _lobbyViewController;

        private void Awake()
        {
        }

        private void Start()
        {
            ShowScreen(UIScreen.Lobby, true);
        }

        public void ShowScreen(UIScreen screen, bool show)
        {
            switch (screen)
            {
                case UIScreen.Lobby:
                    _lobbyViewController.Show(show);
                    HideAllScreensExcept(lobby: true);
                    break;
                default:
                    Debug.LogWarning("Unknown screen: " + screen);
                    break;
            }
        }

        private void HideAllScreensExcept(bool lobby = false)
        {
            if (!lobby) _lobbyViewController.Show(false);
        }
    }
}
