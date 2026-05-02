using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class HUDDriverViewController : RaceBindTarget
    {
        [SerializeField] private UIController_Race _uiControllerRace;
        [SerializeField] private UIDocument _hudDriverDocument;
        [SerializeField] private RaceBindTarget[] _raceBindTargets;

        #region Bindings
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


        private VisualElement _root;



        private void OnEnable()
        {
            _root = _hudDriverDocument.rootVisualElement;
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
