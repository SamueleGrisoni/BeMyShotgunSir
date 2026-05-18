using BeMyShotgunSir.Scripts.Core;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{

    public enum UIScreen
    {
        None = 0,
        CentralHub,
        HostOrJoin,
        Lobby,
        SettingsMenu,
        CustomizationMenu,
        ShopMenu,
        HUDDriver,
        HUDShotgun
    }

    public enum UIOverlay
    {
        Achievements,
        Leaderboards,
        PrivateLobby
    }

    public class UIControllerInit : MonoBehaviour
    {
        [Header("UI Controllers")]
        [Header("Central Hub")]
        [SerializeField] private CentralHubViewController _centralHubViewController;
        [Header("Host Or Join Menu")]
        [SerializeField] private HostOrJoinViewController _hostOrJoinViewController;

        private UIScreen _nextScreen;

        private void Start()
        {
            _nextScreen = GameServices.Instance.UIFlowState.GetNextState();

            if (_nextScreen != UIScreen.None)
            {
                ShowScreen(_nextScreen, true);
                return;
            }
            ShowScreen(UIScreen.CentralHub, true);
        }

        public void ShowScreen(UIScreen screen, bool show)
        {
            switch (screen)
            {
                case UIScreen.CentralHub:
                    _centralHubViewController.Show(show);
                    HideAllScreensExcept(centralHub: true);
                    break;
                case UIScreen.HostOrJoin:
                    _hostOrJoinViewController.Show(show);
                    HideAllScreensExcept(hostOrJoin: true);
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

        private void HideAllScreensExcept(bool centralHub = false, bool hostOrJoin = false)
        {
            if (!centralHub) _centralHubViewController.Show(false);
            if (!hostOrJoin) _hostOrJoinViewController.Show(false);
        }


    }
}
