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
        [Header("UI References")]
        [SerializeField] private UIDocument _hudShotgunDocument;
        [SerializeField] private TopBarViewController _topBarViewController;
        [SerializeField] private InfoBarViewController _infoBarViewController;
        [SerializeField] private ShoutWheelViewController _shoutWheelViewController;
        [SerializeField] private RaceMapViewController _raceMapViewController;
        [SerializeField] private PowerUpViewController _powerUpViewController;

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
        private VisualElement _buttonContainer;
        private Button _shoutWheelButton;
        private Button _raceMapButton;
        #endregion

        private void OnEnable()
        {
            _root = _hudShotgunDocument.rootVisualElement;
            _buttonContainer = _root.Q<VisualElement>("ButtonContainer");
            _shoutWheelButton = _buttonContainer.Q<Button>("ShoutWheelButton");
            _raceMapButton = _buttonContainer.Q<Button>("MapButton");

            StartCoroutine(InitNextFrame());

        }

        IEnumerator InitNextFrame()
        {
            yield return null;

            // _shoutWheelButton.RegisterCallback<PointerDownEvent>(ShoutWheelButtonHandler);
            _raceMapButton.clicked += RaceMapButtonHandler;

        }

        // private void ShowShoutWheel(bool show) => _shoutWheelViewController.Show(show);
        private void RaceMapButtonHandler() => _raceMapViewController.Show(!_raceMapViewController.IsShowing);


        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

    }
}
