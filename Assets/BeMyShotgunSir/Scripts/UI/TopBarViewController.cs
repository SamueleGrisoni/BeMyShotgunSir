using System.Collections;
using System.Collections.Generic;
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
        private VisualElement _markerRow;

        private int? _teamId;
        private Coroutine _markerLerpCoroutine;

        private readonly Dictionary<int, TeamMarkerView> _teamMarkers = new();

        private class TeamMarkerView
        {
            public VisualElement Marker;
            public VisualElement Icon;
            public float CurrentPosition;
            public float TargetPosition;
        }

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

            _viewModel.OnTeamTrackProgressChanged += TeamTrackProgressChangedHandler;
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
                Log.ELazy(() => "Top Bar Document reference missing!", this);
                return;
            }

            _root = _topBarDocument.rootVisualElement;
            _mapBarContainer = _root.Q<VisualElement>("MapBarContainer");
            _markerRow = _root.Q<VisualElement>("MarkerRow");

            if (_mapBarContainer == null || _markerRow == null)
            {
                Log.ELazy(() => "Top bar visual elements not found!", this);
                return;
            }

            ClearMarkers();

            // SOInvisibility_PU.OnInvisibilityEffectApplied += OnInvisibilityEffectApplied;
        }

        private void OnDisable()
        {
            if (_markerLerpCoroutine != null)
            {
                StopCoroutine(_markerLerpCoroutine);
                _markerLerpCoroutine = null;
            }

            // SOInvisibility_PU.OnInvisibilityEffectApplied -= OnInvisibilityEffectApplied;
        }

        private void ClearMarkers()
        {
            _teamMarkers.Clear();
            _markerRow.Clear();
        }

        private void AssignMarkers()
        {
            foreach (KeyValuePair<int, TeamTrackProgress> teamsIdValuePair in _viewModel.TeamTrackProgress)
            {
                if (_teamMarkers.ContainsKey(teamsIdValuePair.Key))
                    continue;

                CreateTeamMarker(teamsIdValuePair.Key);
            }

            ScaleMyTeam();

            if (_markerLerpCoroutine == null && isActiveAndEnabled)
                _markerLerpCoroutine = StartCoroutine(LerpMarkersRoutine());
        }

        private void CreateTeamMarker(int teamIndex)
        {
            if (_teamMarkers.ContainsKey(teamIndex))
                return;

            var marker = new VisualElement
            {
                name = $"Team{teamIndex}Marker"
            };

            marker.AddToClassList("team-marker");
            marker.AddToClassList($"team-marker-{teamIndex}");

            var icon = new VisualElement
            {
                name = $"Team{teamIndex}Icon"
            };

            icon.AddToClassList("team-icon");

            marker.Add(icon);
            _markerRow.Add(marker);

            var markerView = new TeamMarkerView
            {
                Marker = marker,
                Icon = icon,
                CurrentPosition = 0f,
                TargetPosition = 0f
            };

            _teamMarkers.Add(teamIndex, markerView);

            SetMarkerPosition(marker, 0f);
        }

        private IEnumerator LerpMarkersRoutine()
        {
            while (true)
            {
                foreach (TeamMarkerView markerView in _teamMarkers.Values)
                {
                    markerView.CurrentPosition = Mathf.Lerp(
                        markerView.CurrentPosition,
                        markerView.TargetPosition,
                        Time.unscaledDeltaTime * _markerLerpSpeed
                    );

                    SetMarkerPosition(markerView.Marker, markerView.CurrentPosition);
                }

                yield return null;
            }
        }

        private void TeamTrackProgressChangedHandler()
        {
            AssignMarkers();

            IReadOnlyDictionary<int, TeamTrackProgress> teamTrackProgress = new Dictionary<int, TeamTrackProgress>(_viewModel.TeamTrackProgress);

            foreach (KeyValuePair<int, TeamTrackProgress> teamsIdValuePair in teamTrackProgress)
            {
                UpdateTeamMarker(teamsIdValuePair.Key, _viewModel.TeamTrackProgress[teamsIdValuePair.Key]);
            }
        }

        private void UpdateTeamMarker(int teamIndex, TeamTrackProgress progress)
        {
            if (!_teamMarkers.TryGetValue(teamIndex, out TeamMarkerView markerView))
                return;

            float normalizedPosition = GetSectionNormalizedPosition(progress);
            markerView.TargetPosition = normalizedPosition;
        }

        private void SetMarkerPosition(VisualElement marker, float position)
        {
            if (marker == null)
                return;

            marker.style.left = new StyleLength(new Length(position * 100f, LengthUnit.Percent));
        }

        private void ScaleMyTeam()
        {
            if (_teamId == null)
                return;

            foreach (KeyValuePair<int, TeamMarkerView> pair in _teamMarkers)
            {
                if (pair.Key == _teamId.Value)
                    pair.Value.Marker.AddToClassList("my-team");
                else
                    pair.Value.Marker.RemoveFromClassList("my-team");
            }
        }

        private float GetSectionNormalizedPosition(TeamTrackProgress progress)
        {
            int currentChunkId = progress.CurrentChunkId;
            int nextSpecialChunkId = progress.NextSpecialChunkId;

            if (!progress.LastSpecialChunkType.HasValue)
                return 0f;

            int lastSpecialChunkId = progress.LastSpecialChunkType.Value.Id;

            if (nextSpecialChunkId <= lastSpecialChunkId)
                return 0f;

            return Mathf.Clamp01(
                (float)(currentChunkId - lastSpecialChunkId) /
                (nextSpecialChunkId - lastSpecialChunkId)
            );
        }

        private void OnInvisibilityEffectApplied(bool isActive)
        {
            if (_teamId == null)
                return;

            if (_teamMarkers.TryGetValue(_teamId.Value, out TeamMarkerView markerView))
                markerView.Marker.style.opacity = isActive ? 0f : 1f;
        }

        public void ChangeTeamIcon(int teamIndex, Sprite newIcon)
        {
            if (!_teamMarkers.TryGetValue(teamIndex, out TeamMarkerView markerView))
                return;

            markerView.Icon.style.backgroundImage = new StyleBackground(newIcon);
        }
    }
}
