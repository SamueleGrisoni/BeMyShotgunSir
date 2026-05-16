using System.Collections;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class FinishScreenViewController : RaceBindTarget
    {
        [SerializeField] private UIDocument _finishScreenDocument;

        #region Bindings
        private RaceCommand _command;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
        private IInputPublisher _inputPublisher;
        private RaceRole _role;
        #endregion

        private VisualElement _root;
        private Label _resultLabel;
        private Label _subtitleLabel;
        private Button _backToTitleButton;

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

            _viewModel.OnShowFinishScreenChanged += ShowResultFromLeaderboard;
        }

        private void OnEnable()
        {
            if (_finishScreenDocument == null)
            {
                Log.ELazy(() => "Finish Screen Document is null.", this);
                return;
            }

            _root = _finishScreenDocument.rootVisualElement;
            _resultLabel = _root.Q<Label>("ResultLabel");
            _subtitleLabel = _root.Q<Label>("SubtitleLabel");
            _backToTitleButton = _root.Q<Button>("BackToTitleButton");

            StartCoroutine(InitNextFrame());

            Hide();
        }

        IEnumerator InitNextFrame()
        {
            yield return null;

            if (_backToTitleButton != null)
                _backToTitleButton.clicked += BackToTitleHandler;
        }

        private void OnDisable()
        {
            if (_viewModel != null)
                _viewModel.OnShowFinishScreenChanged -= ShowResultFromLeaderboard;

            if (_backToTitleButton != null)
                _backToTitleButton.clicked -= BackToTitleHandler;
        }

        private void ShowResultFromLeaderboard()
        {
            int winnerTeamId = _viewModel.Leaderboard[0];
            int localTeamId = _viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId) ? teamId ?? -1 : -1;

            Show(winnerTeamId == localTeamId);
        }

        public void Show(bool hasWon)
        {
            if (_root == null || _resultLabel == null)
                return;

            _root.style.display = DisplayStyle.Flex;

            _root.RemoveFromClassList("hidden");
            _root.RemoveFromClassList("win");
            _root.RemoveFromClassList("lose");

            if (hasWon)
            {
                _root.AddToClassList("win");
                _resultLabel.text = "YOU WIN";

                if (_subtitleLabel != null)
                    _subtitleLabel.text = "First place!";
            }
            else
            {
                _root.AddToClassList("lose");
                _resultLabel.text = "YOU LOSE";

                if (_subtitleLabel != null)
                    _subtitleLabel.text = "Better luck next time!";
            }
        }

        public void Hide()
        {
            if (_root == null)
                return;

            _root.style.display = DisplayStyle.None;
            _root.AddToClassList("hidden");
            _root.RemoveFromClassList("win");
            _root.RemoveFromClassList("lose");
        }

        private void BackToTitleHandler()
        {
            Hide();

            // _command.BackToTitle();
        }
    }
}
