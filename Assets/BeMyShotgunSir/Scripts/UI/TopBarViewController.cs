using System.Collections;
using BeMyShotgunSir.Gameplay.PowerUps;
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
        [SerializeField] private float _markerLerpSpeed = 8f;

        private RaceCommand _command;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
        private IInputPublisher _inputPublisher;
        private RaceRole _role;

        private VisualElement _root;
        private VisualElement _mapBarContainer;
        private VisualElement _team1Marker;
        private VisualElement _team1Icon;
        private VisualElement _team2Marker;
        private VisualElement _team2Icon;

        private float _team1CurrentPosition;
        private float _team1TargetPosition;
        private float _team2CurrentPosition;
        private float _team2TargetPosition;
        private int? _teamId;

        private Coroutine _markerLerpCoroutine;

        public override void OnInitialBindComplete()
        {
            if (_initialBindSource == null)
            {
                Log.ELazy(() => "Initial bind source is null. Cannot complete initial bind.", this);
                return;
            }

            _command = _initialBindSource.Command;
            _viewModel = _initialBindSource.ViewModel;

            _teamId = _viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId) ? teamId : null;
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

        private void OnEnable()
        {
            if (_topBarDocument == null)
            {
                Debug.LogError("Top Bar Document reference missing!", this);
                return;
            }

            _root = _topBarDocument.rootVisualElement;
            _mapBarContainer = _root.Q<VisualElement>("MapBarContainer");

            if (_mapBarContainer == null)
            {
                Debug.LogError("MapBarContainer not found!", this);
                return;
            }

            _team1Marker = _mapBarContainer.Q<VisualElement>("Team1Marker");
            _team1Icon = _mapBarContainer.Q<VisualElement>("Team1Icon");
            _team2Marker = _mapBarContainer.Q<VisualElement>("Team2Marker");
            _team2Icon = _mapBarContainer.Q<VisualElement>("Team2Icon");

            ScaleMyTeam(_teamId);
            SetMarkerImmediate(_team1Marker, 0f);
            SetMarkerImmediate(_team2Marker, 0f);

            _markerLerpCoroutine = StartCoroutine(LerpMarkersRoutine());

            // SOInvisibility_PU.OnInvisibilityEffectApplied += OnInvisibilityEffectApplied;

        }

        private void OnDisable()
        {
            if (_markerLerpCoroutine != null)
            {
                StopCoroutine(_markerLerpCoroutine);
                _markerLerpCoroutine = null;
            }
        }

        private IEnumerator LerpMarkersRoutine()
        {
            while (true)
            {
                _team1CurrentPosition = Mathf.Lerp(
                    _team1CurrentPosition,
                    _team1TargetPosition,
                    Time.unscaledDeltaTime * _markerLerpSpeed
                );

                _team2CurrentPosition = Mathf.Lerp(
                    _team2CurrentPosition,
                    _team2TargetPosition,
                    Time.unscaledDeltaTime * _markerLerpSpeed
                );

                SetMarkerPosition(_team1Marker, _team1CurrentPosition);
                SetMarkerPosition(_team2Marker, _team2CurrentPosition);

                yield return null;
            }
        }

        private void ScaleMyTeam(int? teamId)
        {
            if (teamId == null)
                return;

            switch (teamId)
            {
                case 0:
                    _team1Marker.AddToClassList("my-team");
                    _team2Marker.RemoveFromClassList("my-team");
                    break;
                case 1:
                    _team2Marker.AddToClassList("my-team");
                    _team1Marker.RemoveFromClassList("my-team");
                    break;
            }
        }

        private void SetMarkerImmediate(VisualElement marker, float position)
        {
            SetMarkerPosition(marker, position);
        }

        private void SetMarkerPosition(VisualElement marker, float position)
        {
            if (marker == null)
                return;

            marker.style.left = new StyleLength(new Length(position * 100f, LengthUnit.Percent));
        }

        private void ChangeTeamIcon(int teamIndex, Sprite newIcon)
        {
            if (teamIndex == 0 && _team1Icon != null)
                _team1Icon.style.backgroundImage = new StyleBackground(newIcon);
            else if (teamIndex == 1 && _team2Icon != null)
                _team2Icon.style.backgroundImage = new StyleBackground(newIcon);
        }

        private float GetSectionNormalizedPosition(TeamTrackProgress progress)
        {
            int currentChunkId = progress.CurrentChunkId;
            int nextSpecialChunkId = progress.NextSpecialChunkId;

            int lastSpecialChunkId = progress.LastSpecialChunkType.Value.Id;

            if (nextSpecialChunkId <= lastSpecialChunkId)
                return 0f;

            if (currentChunkId <= lastSpecialChunkId)
                return 0f;

            if (currentChunkId >= nextSpecialChunkId)
                return 1f;

            float sectionLength = nextSpecialChunkId - lastSpecialChunkId;
            float currentSectionProgress = currentChunkId - lastSpecialChunkId;

            return Mathf.Clamp01(currentSectionProgress / sectionLength);
        }

        private void OnInvisibilityEffectApplied(bool isActive)
        {
            switch (_teamId)
            {
                case 0:
                    _team1Marker.style.opacity = isActive ? 0f : 1f;
                    break;
                case 1:
                    _team2Marker.style.opacity = isActive ? 0f : 1f;
                    break;
            }
        }

        public void UpdateTeamMarker(int teamIndex, TeamTrackProgress progress)
        {
            float normalizedPosition = GetSectionNormalizedPosition(progress);

            if (teamIndex == 0)
                _team1TargetPosition = normalizedPosition;
            else if (teamIndex == 1)
                _team2TargetPosition = normalizedPosition;
        }
    }
}
