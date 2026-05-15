using BeMyShotgunSir.Scripts.Events;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    public class UIController_Bootstrap : MonoBehaviour
    {
        [SerializeField] private LoadingScreenViewController _loadingScreenViewController;
        [SerializeField] private SOLoadingRequestEvent _loadingRequestEvent;


        private void OnEnable()
        {
            _loadingRequestEvent.OnEventRaised += ShowLoadingScreen;
        }


        private void ShowLoadingScreen(IEventSender sender, bool show)
        {
            _loadingScreenViewController.Show(show);
        }

    }

}
