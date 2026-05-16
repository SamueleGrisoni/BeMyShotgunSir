using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class HUDDriverViewController : RaceBindTarget
    {
        [SerializeField] private UIController_Race _uiControllerRace;
        [SerializeField] private UIDocument _hudDriverDocument;
        [SerializeField] private TopBarViewController _topBarViewController;

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
        }


        private VisualElement _root;

        private void OnEnable()
        {
            _root = _hudDriverDocument.rootVisualElement;
        }

        private void UpdateTeamProgress()
        {
            // IReadOnlyDictionary<int, TeamTrackProgress> teamTrackProgress = new Dictionary<int, TeamTrackProgress>(_viewModel.TeamTrackProgress);

            // foreach (KeyValuePair<int, TeamTrackProgress> teamsIdValuePair in teamTrackProgress)
            // {
            //     _topBarViewController.UpdateTeamMarker(teamsIdValuePair.Key, _viewModel.TeamTrackProgress[teamsIdValuePair.Key]);
            // }
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
