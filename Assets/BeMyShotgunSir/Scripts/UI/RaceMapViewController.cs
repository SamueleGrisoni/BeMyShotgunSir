using System.Collections;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{

    public class RaceMapViewController : RaceBindTarget
    {
        private struct CachedSplitMap
        {
            public int StartChunkId;
            public int EndChunkId;
            public List<GeneratedRoadChunkInfoWithItems> SplitData;

            public CachedSplitMap(
                int startChunkId,
                int endChunkId,
                List<GeneratedRoadChunkInfoWithItems> splitData
            )
            {
                StartChunkId = startChunkId;
                EndChunkId = endChunkId;
                SplitData = splitData;
            }
        }

        [SerializeField] private UIDocument _hudDocument;
        [SerializeField] private RoadChunkTile[] _tilesRC;
        [SerializeField] private RoadChunkTile _forkTileRC;
        [SerializeField] private RoadChunkTile _junctionTileRC;
        [Header("Fog Overlay")]
        [SerializeField] private Sprite[] _fogSprites;
        [SerializeField] private int _fogColumns = 6;
        [SerializeField] private int _fogRows = 10;
        [SerializeField] private float _revealStepDelay = 0.15f;
        [Header("Power-Ups")]
        [SerializeField] private SOPowerUpIcons _powerUpIcons;
        [SerializeField] private SOPowerUpsData _powerUpsData;


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
        private TemplateContainer _raceMap;
        private VisualElement _mapTiles;
        private VisualElement _fogGrid;
        private VisualElement[,] _fogTiles;
        private VisualElement _powerUpArea;
        private VisualElement _leftColumn;
        private VisualElement _rightColumn;

        #endregion

        #region private fields
        private int _tileLeftAnchorOut;
        private int _tileRightAnchorOut;

        private List<GeneratedRoadChunkInfoWithItems> _currentSplitData = new List<GeneratedRoadChunkInfoWithItems>();
        private readonly List<GeneratedItemInfo> _leftPowerUps = new();
        private readonly List<GeneratedItemInfo> _rightPowerUps = new();

        private readonly List<CachedSplitMap> _cachedSplitMaps = new();
        private CachedSplitMap? _currentCachedMap;
        private int _revealedFogSteps;
        private readonly List<Vector2Int> _leftFogRevealOrder = new();
        private readonly List<Vector2Int> _rightFogRevealOrder = new();


        private bool _isShowing = false;
        #endregion

        #region Public Fields
        public bool IsShowing => _isShowing;
        #endregion


        private void Awake()
        {
            RoadManager.OnSplitGeneratedProvided += SplitGeneratedHandler;
            Log.DLazy(() => "RaceMapViewController enabled and subscribed to OnSplitGeneratedProvided event.", this);
        }

        private void OnEnable()
        {
            if (_hudDocument == null)
            {
                Debug.Log("Race Map Document reference missing!");
                return;
            }

            _root = _hudDocument.rootVisualElement;
            _raceMap = _root.Q<TemplateContainer>("RaceMap");
            _mapTiles = _raceMap.Q<VisualElement>("MapTiles");
            _fogGrid = _raceMap.Q<VisualElement>("FogGrid");
            _powerUpArea = _raceMap.Q<VisualElement>("PowerUpArea");
            _leftColumn = _raceMap.Q<VisualElement>("LeftColumn");
            _rightColumn = _raceMap.Q<VisualElement>("RightColumn");

            BuildFogGrid();
            BuildFogRevealOrder();
            ResetFogRevealProgress();

            Show(false);
        }

        #region Handlers
        private void SplitGeneratedHandler(List<GeneratedRoadChunkInfoWithItems> splitData)
        {
            if (splitData == null)
            {
                Log.ELazy(() => "Split data is null. Cannot handle split generated event.", this);
                return;
            }

            if (!TryGetSplitBounds(splitData, out int startChunkId, out int endChunkId))
            {
                Log.ELazy(() => "Cannot cache split map because bounds are invalid.", this);
                return;
            }

            _cachedSplitMaps.Add(new CachedSplitMap(startChunkId, endChunkId, splitData));

            TryLoadMapForCurrentTeamProgress();
        }
        #endregion

        #region Race Map Construction and Update Methods
        private void BuildRaceMap()
        {
            var forkTile = new VisualElement();
            forkTile.AddToClassList("rc-icon");
            forkTile.AddToClassList("fork");
            forkTile.style.backgroundImage = new StyleBackground(_forkTileRC.Icon);

            var forkRow = new VisualElement();
            forkRow.AddToClassList("row");
            forkRow.AddToClassList("row-fork");
            forkRow.Add(forkTile);

            _mapTiles.Add(forkRow);
            _tileLeftAnchorOut = _forkTileRC.AnchorInPx;
            _tileRightAnchorOut = _forkTileRC.AnchorOutPx;

            BuildRowsFromSplit();

            var junctionTile = new VisualElement();
            junctionTile.AddToClassList("rc-icon");
            junctionTile.AddToClassList("junction");
            junctionTile.style.backgroundImage = new StyleBackground(_junctionTileRC.Icon);

            var junctionRow = new VisualElement();
            junctionRow.AddToClassList("row");
            junctionRow.AddToClassList("row-junction");
            junctionRow.Add(junctionTile);

            _mapTiles.Add(junctionRow);
        }

        private void BuildRowsFromSplit()
        {
            int prevAnchorLeft = _tileLeftAnchorOut;
            int prevAnchorRight = _tileRightAnchorOut;

            VisualElement currentRow = null;
            int chunksInCurrentRow = 0;

            foreach (GeneratedRoadChunkInfoWithItems data in _currentSplitData)
            {
                GeneratedRoadChunkInfo info = data.roadChunkInfo;

                if (info.position == RoadChunkPosition.MIDDLE) continue;

                ExtractPowerUps(data);

                int tileIndex = GetTileIndex(info);

                if (tileIndex < 0 || tileIndex >= _tilesRC.Length)
                {
                    Debug.LogError($"Invalid tile index {tileIndex} for road chunk type {info.type} at position {info.position}");
                    continue;
                }

                if (currentRow == null)
                {
                    currentRow = new VisualElement();
                    currentRow.AddToClassList("row");
                    chunksInCurrentRow = 0;
                }

                if (info.position == RoadChunkPosition.LEFT)
                {
                    AddChunkToRow(currentRow, tileIndex, prevAnchorLeft);
                    prevAnchorLeft = prevAnchorLeft + _tilesRC[tileIndex].AnchorOutPx;
                    _tileLeftAnchorOut = prevAnchorLeft;
                }
                else if (info.position == RoadChunkPosition.RIGHT)
                {
                    AddChunkToRow(currentRow, tileIndex, prevAnchorRight);
                    prevAnchorRight = prevAnchorRight + _tilesRC[tileIndex].AnchorOutPx;
                    _tileRightAnchorOut = prevAnchorRight;
                }

                chunksInCurrentRow++;

                if (chunksInCurrentRow == 2)
                {
                    _mapTiles.Add(currentRow);
                    currentRow = null;
                    chunksInCurrentRow = 0;
                }


            }
        }


        private int GetTileIndex(GeneratedRoadChunkInfo info)
        {
            if (info.type == RoadChunkType.STRAIGHT)
                return 8; // straight tile index
            else return info.index;
        }

        private void AddChunkToRow(VisualElement row, int tileIndex, int prevAnchor)
        {
            int tileX = prevAnchor + _tilesRC[tileIndex].AnchorInPx;

            var chunk = new VisualElement();
            chunk.AddToClassList("rc-icon");
            chunk.style.backgroundImage = new StyleBackground(_tilesRC[tileIndex].Icon);
            chunk.style.left = tileX;

            row.Add(chunk);
        }

        private void ClearRaceMap()
        {
            while (_mapTiles.childCount > 0)
            {
                _mapTiles.RemoveAt(0);
            }
        }
        #endregion

        #region Map Caching and Loading Methods

        private bool TryGetSplitBounds(List<GeneratedRoadChunkInfoWithItems> splitData, out int startChunkId, out int endChunkId)
        {
            startChunkId = -1;
            endChunkId = -1;

            foreach (GeneratedRoadChunkInfoWithItems data in splitData)
            {
                GeneratedRoadChunkInfo info = data.roadChunkInfo;

                if (info.type == RoadChunkType.STARTING_CROSSROAD)
                    startChunkId = info.chunkNumber;

                if (info.type == RoadChunkType.ENDING_CROSSROAD)
                    endChunkId = info.chunkNumber;
            }

            return startChunkId >= 0 && endChunkId > startChunkId;
        }

        private void TryLoadMapForCurrentTeamProgress()
        {
            if (_viewModel == null)
                return;

            bool hasTeamId = _viewModel.TryGetTeamIdFromClientId(
                _viewModel.ClientId,
                out int? teamId
            );

            if (!hasTeamId || teamId == null)
                return;

            if (!_viewModel.TeamTrackProgress.TryGetValue((int)teamId, out TeamTrackProgress progress))
                return;

            TryLoadMapForProgress(progress);
        }

        private void TryLoadMapForProgress(TeamTrackProgress progress)
        {
            CachedSplitMap? targetMap = FindMapForProgress(progress);

            if (targetMap == null)
                return;

            if (_currentCachedMap.HasValue &&
                _currentCachedMap.Value.StartChunkId == targetMap.Value.StartChunkId &&
                _currentCachedMap.Value.EndChunkId == targetMap.Value.EndChunkId)
            {
                return;
            }

            LoadCachedMap(targetMap.Value);
        }

        private CachedSplitMap? FindMapForProgress(TeamTrackProgress progress)
        {
            for (int i = 0; i < _cachedSplitMaps.Count; i++)
            {
                CachedSplitMap map = _cachedSplitMaps[i];

                if (!progress.LastSpecialChunkType.HasValue)
                    return null;

                if ((progress.LastSpecialChunkType.Value.Type == RoadChunkType.START_LINE ||
                    progress.LastSpecialChunkType.Value.Type == RoadChunkType.ENDING_CROSSROAD) && progress.NextSpecialChunkId == map.StartChunkId)
                {
                    return map;
                }
            }

            return null;
        }
        private void LoadCachedMap(CachedSplitMap cachedMap)
        {
            _currentCachedMap = cachedMap;
            _currentSplitData = cachedMap.SplitData;

            ClearRaceMap();
            ClearPowerUps();

            BuildRaceMap();
            PopulatePowerUps();

            ResetFogRevealProgress();
            BuildFogRevealOrder();
        }

        #endregion

        #region Fog Methods

        private void UpdateFogFromProgress(TeamTrackProgress progress)
        {
            if (!progress.LastSpecialChunkType.HasValue)
                return;

            PortalInfo lastSpecial = progress.LastSpecialChunkType.Value;

            if (lastSpecial.Type != RoadChunkType.START_LINE &&
                lastSpecial.Type != RoadChunkType.ENDING_CROSSROAD)
                return;

            int start = lastSpecial.Id;
            int end = progress.NextSpecialChunkId;
            int current = progress.CurrentChunkId;

            if (end <= start)
                return;

            float traveledPercent = Mathf.InverseLerp(start, end, current);
            float fogPercentage = (1f - traveledPercent) * 100f;

            SetFogFromRoadPercentage(fogPercentage);
        }

        private void SetFogFromRoadPercentage(float roadPercentage)
        {
            roadPercentage = Mathf.Clamp(roadPercentage, 0f, 100f);

            float revealedPercent = 1f - roadPercentage / 100f;

            int maxSteps = Mathf.Max(
                _leftFogRevealOrder.Count,
                _rightFogRevealOrder.Count
            );

            int targetSteps = Mathf.CeilToInt(revealedPercent * maxSteps);

            if (targetSteps <= _revealedFogSteps)
                return;

            RevealFogSteps(_revealedFogSteps, targetSteps);
            _revealedFogSteps = targetSteps;
        }

        private void RevealFogSteps(int fromStep, int toStep)
        {
            for (int i = fromStep; i < toStep; i++)
            {
                if (i < _leftFogRevealOrder.Count)
                {
                    Vector2Int tile = _leftFogRevealOrder[i];
                    RevealFogTile(tile.x, tile.y);
                }

                if (i < _rightFogRevealOrder.Count)
                {
                    Vector2Int tile = _rightFogRevealOrder[i];
                    RevealFogTile(tile.x, tile.y);
                }
            }
        }

        private void BuildFogRevealOrder()
        {
            _leftFogRevealOrder.Clear();
            _rightFogRevealOrder.Clear();

            int halfColumns = _fogColumns / 2;

            for (int y = 0; y < _fogRows; y++)
            {
                for (int x = 0; x < _fogColumns; x++)
                {
                    if (x < halfColumns)
                        _leftFogRevealOrder.Add(new Vector2Int(x, y));
                    else
                        _rightFogRevealOrder.Add(new Vector2Int(x, y));
                }
            }

            Shuffle(_leftFogRevealOrder);
            Shuffle(_rightFogRevealOrder);
        }

        private void ResetFogRevealProgress()
        {
            _revealedFogSteps = 0;

            if (_fogTiles == null)
                return;

            for (int y = 0; y < _fogRows; y++)
            {
                for (int x = 0; x < _fogColumns; x++)
                {
                    _fogTiles[x, y]?.RemoveFromClassList("revealed");
                }
            }
        }

        private void BuildFogGrid()
        {
            if (_fogGrid == null)
                return;

            _fogGrid.Clear();

            _fogTiles = new VisualElement[_fogColumns, _fogRows];

            float tileWidth = 100f / _fogColumns;
            float tileHeight = 100f / _fogRows;

            for (int y = 0; y < _fogRows; y++)
            {
                for (int x = 0; x < _fogColumns; x++)
                {
                    int index = y * _fogColumns + x;

                    if (index < 0 || index >= _fogSprites.Length)
                        continue;

                    var fogTile = new VisualElement();
                    fogTile.AddToClassList("fog-tile");

                    fogTile.style.left = Length.Percent(x * tileWidth);
                    fogTile.style.top = Length.Percent(y * tileHeight);
                    fogTile.style.width = Length.Percent(tileWidth);
                    fogTile.style.height = Length.Percent(tileHeight);

                    fogTile.style.backgroundImage = new StyleBackground(_fogSprites[index]);

                    _fogGrid.Add(fogTile);
                    _fogTiles[x, y] = fogTile;
                }
            }
        }

        private void RevealFogTile(int x, int y)
        {
            if (x < 0 || x >= _fogColumns) return;
            if (y < 0 || y >= _fogRows) return;

            _fogTiles[x, y]?.AddToClassList("revealed");
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        #endregion

        #region Power-Up Methods

        private void ClearPowerUps()
        {
            _leftPowerUps.Clear();
            _rightPowerUps.Clear();

            _leftColumn?.Clear();
            _rightColumn?.Clear();
        }

        private void ExtractPowerUps(GeneratedRoadChunkInfoWithItems data)
        {
            if (data.itemsToSpawn == null || data.itemsToSpawn.Count == 0)
                return;

            foreach (GeneratedItemInfo item in data.itemsToSpawn)
            {
                if (item.type != ItemType.POWER_UP)
                    continue;

                if (data.roadChunkInfo.position == RoadChunkPosition.LEFT)
                {
                    _leftPowerUps.Add(item);
                }
                else if (data.roadChunkInfo.position == RoadChunkPosition.RIGHT)
                {
                    _rightPowerUps.Add(item);
                }
            }
        }

        private void PopulatePowerUps()
        {
            PopulateColumn(_leftColumn, _leftPowerUps);
            PopulateColumn(_rightColumn, _rightPowerUps);
        }

        private void PopulateColumn(VisualElement column, List<GeneratedItemInfo> items)
        {
            column.Clear();

            foreach (GeneratedItemInfo item in items)
            {
                Sprite icon = GetPowerUpIcon(item);

                if (icon == null)
                    continue;

                var iconElement = new VisualElement();

                iconElement.AddToClassList("power-up-icon");
                iconElement.style.backgroundImage = new StyleBackground(icon);

                column.Add(iconElement);
            }
        }

        private Sprite GetPowerUpIcon(GeneratedItemInfo item)
        {
            if (item.type != ItemType.POWER_UP)
                return null;

            PowerUp powerUp = _powerUpsData.PowerUps[item.index].PowerUpType;
            return _powerUpIcons.GetIcon(powerUp);
        }

        #endregion


        #region Public Methods
        public void Show(bool show)
        {
            _raceMap.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _isShowing = show;
        }

        public void UpdateMapFromTeamProgress(TeamTrackProgress progress)
        {
            TryLoadMapForProgress(progress);
            UpdateFogFromProgress(progress);
        }

        #endregion
    }
}
