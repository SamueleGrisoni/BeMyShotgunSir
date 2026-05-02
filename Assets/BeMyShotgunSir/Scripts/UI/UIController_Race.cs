
using UnityEngine;


namespace BeMyShotgunSir.Scripts.UI
{
    public class UIController_Race : MonoBehaviour
    {
        [Header("UI Controllers")]
        [Header("HUD Driver")]
        [SerializeField] private HUDDriverViewController _HUDDriverViewController;

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
                    _HUDDriverViewController.Show(show);
                    HideAllScreensExcept(lobby: true);
                    break;
                default:
                    Debug.LogWarning("Unknown screen: " + screen);
                    break;
            }
        }

        private void HideAllScreensExcept(bool lobby = false)
        {
            if (!lobby) _HUDDriverViewController.Show(false);
        }
    }
}
