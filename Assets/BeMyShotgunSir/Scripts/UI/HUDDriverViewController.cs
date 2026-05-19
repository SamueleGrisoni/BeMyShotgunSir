using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class HUDDriverViewController : RaceBindTarget
    {
        private bool _log = false;
        [Header("UI References")]
        [SerializeField] private UIController_Race _uiControllerRace;
        [SerializeField] private UIDocument _hudDriverDocument;
        [SerializeField] private TopBarViewController _topBarViewController;
        [SerializeField] private EarlyCommitmentViewController _earlyCommitmentViewController;

        #region Visual Elements
        private VisualElement _root;
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

            if (_role == RaceRole.Driver)
            {
                _viewModel.OnTeamTrackProgressChanged += UpdateTeamProgress;
            }
        }

        private void OnEnable()
        {
            _root = _hudDriverDocument.rootVisualElement;
            TrackGenerator.OnFinishLineGenerated += OnFinishLineGenerated;
        }

        private void OnDisable()
        {
            TrackGenerator.OnFinishLineGenerated -= OnFinishLineGenerated;

            if (_role == RaceRole.Driver)
                _viewModel.OnTeamTrackProgressChanged -= UpdateTeamProgress;
        }

        private void OnFinishLineGenerated(int finishLineChunkId) => _finishLineChunkId = finishLineChunkId;

        private void UpdateTeamProgress()
        {
            if (_finishLineChunkId == -1)
                return;

            _viewModel.TeamTrackProgress.TryGetValue((int)(_viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId) ? teamId : -1), out TeamTrackProgress progress);

            if (!progress.LastSpecialChunkType.HasValue)
                return;

            if (progress.NextSpecialChunkId == _finishLineChunkId)
                _earlyCommitmentViewController.OnFinishRoad();

        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
