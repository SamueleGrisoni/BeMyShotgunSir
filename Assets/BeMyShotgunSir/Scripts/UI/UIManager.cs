using UnityEngine;


namespace BeMyShotgunSir.Scripts.UI

{
    public enum UIScreen
    {
        None = 0,
        CentralHub,
        LobbyMenu,
        SettingsMenu,
        CustomizationMenu,
        ShopMenu,
        HUD
    }

    public enum UIOverlay
    {
        Achievements,
        Leaderboards,
        PrivateLobby
    }

    public class UIManager : MonoBehaviour
    {
        [Header("UI Controllers")]
        [Header("Central Hub")]
        [SerializeField] private CentralHubController _centralHubController;
        [Header("Lobby Menu")]
        [SerializeField] private LobbyViewController _lobbyViewController;


        [Header("HUD")]
        [SerializeField] private HUDController _hudController;

        #region Game Objects
        private GameObject _centralHubGO;
        private GameObject _lobbyMenuGO;
        private GameObject _hudGO;
        #endregion

        private void Awake()
        {
            _centralHubGO = _centralHubController.gameObject;
            _lobbyMenuGO = _lobbyViewController.gameObject;
            _hudGO = _hudController.gameObject;
        }

        private void Start()
        {
            ShowScreen(UIScreen.CentralHub, true);
        }

        public void ShowScreen(UIScreen screen, bool show)
        {
            switch (screen)
            {
                case UIScreen.CentralHub:
                    _centralHubController.Show(show);
                    HideAllScreensExcept(centralHub: true);
                    break;
                case UIScreen.LobbyMenu:
                    _lobbyViewController.Show(show);
                    HideAllScreensExcept(lobbyMenu: true);
                    break;
                case UIScreen.HUD:
                    // _hudController.Show(show);
                    break;
                case UIScreen.SettingsMenu:
                    break;
                case UIScreen.CustomizationMenu:
                    break;
                case UIScreen.ShopMenu:
                    break;
                default:
                    Debug.LogWarning("Unknown screen: " + screen);
                    break;
            }
        }

        private void HideAllScreensExcept(bool centralHub = false, bool lobbyMenu = false, bool hud = false)
        {
            if (!centralHub) _centralHubController.Show(false);
            if (!lobbyMenu) _lobbyViewController.Show(false);
            // if (!hud) _hudController.Show(false);
        }
    }
}
