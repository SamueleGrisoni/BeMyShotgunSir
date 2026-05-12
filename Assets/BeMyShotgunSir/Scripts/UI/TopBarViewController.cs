using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class TopBarViewController : RaceBindTarget
    {
        [SerializeField] private UIDocument _topBarDocument;

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

        #region Visual Elements
        private VisualElement _root;
        private VisualElement _mapBarContainer;
        private VisualElement _team1Marker;
        private VisualElement _team1Icon;
        private VisualElement _team2Marker;
        private VisualElement _team2Icon;
        #endregion



        private void OnEnable()
        {
            if (_topBarDocument == null)
            {
                Debug.Log("Top Bar Document reference missing!");
                return;
            }
            _root = _topBarDocument.rootVisualElement;
            _mapBarContainer = _root.Q<VisualElement>("MapBarContainer");
            _team1Marker = _mapBarContainer.Q<VisualElement>("Team1Marker");
            _team1Icon = _mapBarContainer.Q<VisualElement>("Team1Icon");
            _team2Marker = _mapBarContainer.Q<VisualElement>("Team2Marker");
            _team2Icon = _mapBarContainer.Q<VisualElement>("Team2Icon");

            //TODO: Create position changed event (float par 0-1)
            // _viewModel.OnTeam1PositionChanged += UpdateTeam1Marker;
            // _viewModel.OnTeam2PositionChanged += UpdateTeam2Marker;
            //TODO: Find a way to update icons based on team composition
            // for (int i = 0; i < _viewModel.TeamData.Count; i++)
            // {
            //     _viewModel.TeamData[i].OnIconChanged += ChangeTeamIcon;
            // }
        }

        private void UpdateTeam1Marker(float position) => _team1Marker.style.left = new StyleLength(new Length(position * 100, LengthUnit.Percent));
        private void UpdateTeam2Marker(float position) => _team2Marker.style.left = new StyleLength(new Length(position * 100, LengthUnit.Percent));
        private void ChangeTeamIcon(int teamIndex, Sprite newIcon)
        {
            if (teamIndex == 0)
            {
                _team1Icon.style.backgroundImage = new StyleBackground(newIcon);
            }
            else if (teamIndex == 1)
            {
                _team2Icon.style.backgroundImage = new StyleBackground(newIcon);
            }
        }

    }
}
