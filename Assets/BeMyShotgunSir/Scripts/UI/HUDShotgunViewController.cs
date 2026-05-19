using System.Collections;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class HUDShotgunViewController : RaceBindTarget
    {
        private bool _log = false;

        [Header("UI References")]
        [SerializeField] private UIDocument _hudShotgunDocument;
        [SerializeField] private TopBarViewController _topBarViewController;
        [SerializeField] private InfoBarViewController _infoBarViewController;
        [SerializeField] private ShoutWheelViewController _shoutWheelViewController;
        [SerializeField] private RaceMapViewController _raceMapViewController;
        [SerializeField] private PowerUpViewController _powerUpViewController;

        #region Visual Elements
        private VisualElement _root;
        private VisualElement _buttonContainer;
        private Button _shoutWheelButton;
        private Button _raceMapButton;
        #endregion

        #region private fields
        private int _finishLineChunkId = -1;
        #endregion

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
                Log.ELazy(() => "Initial bind source is null. Cannot complete initial bind.", this, _log);
                return;
            }
            _command = _initialBindSource.Command;
            _viewModel = _initialBindSource.ViewModel;
        }
        public override void OnFinalBindComplete()
        {
            if (_finalBindSource == null)
            {
                Log.ELazy(() => "Final bind source is null. Cannot complete final bind.", this, _log);
                return;
            }
            _roadManager = _finalBindSource.RoadManager;
            _inputPublisher = _finalBindSource.InputPublisher;
            _role = _finalBindSource.Role;

            if (_role == RaceRole.Shotgun)
                _viewModel.OnTeamTrackProgressChanged += UpdateTeamProgress;

        }

        private void OnEnable()
        {
            _root = _hudShotgunDocument.rootVisualElement;
            _buttonContainer = _root.Q<VisualElement>("ButtonContainer");
            _shoutWheelButton = _buttonContainer.Q<Button>("ShoutWheelButton");
            _raceMapButton = _buttonContainer.Q<Button>("MapButton");

            StartCoroutine(InitNextFrame());

            TrackGenerator.OnFinishLineGenerated += OnFinishLineGenerated;
        }

        IEnumerator InitNextFrame()
        {
            yield return null;

            _shoutWheelButton.clicked += ShoutWheelButtonHandler;
            _raceMapButton.clicked += RaceMapButtonHandler;
        }

        private void OnDisable()
        {
            _shoutWheelButton.clicked -= ShoutWheelButtonHandler;
            _raceMapButton.clicked -= RaceMapButtonHandler;

            TrackGenerator.OnFinishLineGenerated -= OnFinishLineGenerated;

            if (_role == RaceRole.Shotgun)
                _viewModel.OnTeamTrackProgressChanged -= UpdateTeamProgress;
        }

        private void ShoutWheelButtonHandler()
        {
            _shoutWheelViewController.Show(!_shoutWheelViewController.IsShowing);
            _raceMapViewController.Show(false);
        }

        private void RaceMapButtonHandler()
        {
            _raceMapViewController.Show(!_raceMapViewController.IsShowing);
            _shoutWheelViewController.Show(false);
        }

        private void OnFinishLineGenerated(int finishLineChunkId) => _finishLineChunkId = finishLineChunkId;

        private void UpdateTeamProgress()
        {
            _viewModel.TeamTrackProgress.TryGetValue((int)(_viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId) ? teamId : -1), out TeamTrackProgress progress);

            if (!progress.LastSpecialChunkType.HasValue)
                return;

            #region debug
            if (progress.NextSpecialChunkId == _finishLineChunkId)
            {
                Log.DLazy(() => "Next special chunk is the finish line!", this, _log);
            }
            #endregion
            Log.DLazy(() => "Current chunk id: " + progress.CurrentChunkId +
                        ", Next special chunk id: " + progress.NextSpecialChunkId +
                        ", Finish line chunk id: " + _finishLineChunkId, this, _log);

            if (progress.CurrentChunkId > progress.LastSpecialChunkType.Value.Id
                && progress.LastSpecialChunkType.Value.Type == RoadChunkType.STARTING_CROSSROAD
                || progress.NextSpecialChunkId == _finishLineChunkId)
            {
                _raceMapButton.style.display = DisplayStyle.None;
                _raceMapViewController.Show(false);
            }
            else
            {
                _raceMapButton.style.display = DisplayStyle.Flex;
                // _raceMapViewController.Show(true); // Uncommment if you want the map to pop up automatically - for debug
                _shoutWheelViewController.Show(false);
            }

            _raceMapViewController.UpdateMapFromTeamProgress(progress);
        }


        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

    }
}
