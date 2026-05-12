
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;


namespace BeMyShotgunSir.Scripts.UI
{
    public class UIController_Race : RaceBindTarget
    {
        [Header("UI Controllers")]
        [Header("HUD Driver")]
        [SerializeField] private HUDDriverViewController _hudDriverViewController;
        [Header("HUD Shotgun")]
        [SerializeField] private HUDShotgunViewController _hudShotgunViewController;

        #region Bindings
        private RaceCommand _command;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
        private IInputPublisher _inputPublisher;
        private RaceRole _role;
        #endregion

        public override void OnInitialBindComplete()
        {
            if (_initialBindSource == null)
            {
                Log.ELazy(() => "Initial bind source is null. Cannot complete initial bind.", this);
                return;
            }
            _command = _initialBindSource.Command;
            _viewModel = _initialBindSource.ViewModel;
        }
        public override void OnFinalBindComplete()
        {
            if (_finalBindSource == null)
            {
                Log.ELazy(() => "Final bind source is null. Cannot complete final bind.", this);
                return;
            }
            _roadManager = _finalBindSource.RoadManager;
            _inputPublisher = _finalBindSource.InputPublisher;
            _role = _finalBindSource.Role;

            switch (_role)
            {
                case RaceRole.Driver:
                    ShowScreen(UIScreen.HUDDriver, true);
                    break;
                case RaceRole.Shotgun:
                    ShowScreen(UIScreen.HUDShotgun, true);
                    break;
                default:
                    Log.ELazy(() => "Unknown role: " + _role, this);
                    break;
            }
        }


        #region private fields
        private GameObject _hudDriverGO;
        private GameObject _hudShotgunGO;
        #endregion

        private void Awake()
        {
            _hudDriverGO = _hudDriverViewController.gameObject;
            _hudShotgunGO = _hudShotgunViewController.gameObject;
        }

        private void Start()
        {
            // ShowScreen(UIScreen.Lobby, true);
        }

        public void ShowScreen(UIScreen screen, bool show)
        {
            switch (screen)
            {
                case UIScreen.HUDDriver:
                    _hudDriverGO.SetActive(show);
                    _hudDriverViewController.Show(show);
                    // HideAllScreensExcept(hudDriver: show);
                    break;
                case UIScreen.HUDShotgun:
                    _hudShotgunGO.SetActive(show);
                    _hudShotgunViewController.Show(show);
                    // HideAllScreensExcept(hudShotgun: show);
                    break;
                default:
                    Log.DLazy(() => "Unknown screen: " + screen, this);
                    break;
            }
        }

        private void HideAllScreensExcept(bool hudDriver = false, bool hudShotgun = false)
        {
            if (!hudDriver) _hudDriverViewController.Show(false);
            if (!hudShotgun) _hudShotgunViewController.Show(false);
        }
    }
}
