
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;


namespace BeMyShotgunSir.Scripts.UI
{
    public class UIController_Race : RaceBindTarget
    {
        [Header("UI Controllers")]
        [Header("HUD Driver")]
        [SerializeField] private HUDDriverViewController _hudDriverViewController;

        #region Bindings
        [SerializeField] private InterfaceSerializer<RaceBindTarget, IRaceBindTarget>[] _bindTargets;
        private IRaceBindTarget[] _coercedTargets;
        private RaceCommand _command;
        private RaceViewModel _viewModel;
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

            //TODO: Race View Model events
            // _viewModel.OnLobbyIPChanged += UpdateLobbyIP;
            // _viewModel.OnPlayerCountChanged += UpdatePlayerCount;
            // _viewModel.OnPlayerStatesChanged += UpdatePlayerStates;
        }
        public override void OnFinalBindComplete()
        {
            if (_finalBindSource == null)
            {
                Log.ELazy(() => "Final bind source is null. Cannot complete final bind.", this);
                return;
            }
        }

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
                    _hudDriverViewController.Show(show);
                    HideAllScreensExcept(lobby: true);
                    break;
                default:
                    Debug.LogWarning("Unknown screen: " + screen);
                    break;
            }
        }

        private void HideAllScreensExcept(bool lobby = false)
        {
            if (!lobby) _hudDriverViewController.Show(false);
        }
    }
}
