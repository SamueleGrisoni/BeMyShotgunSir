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

        private bool _log = false;

        [Header("UI References")]
        [SerializeField] private UIDocument _hudDocument;
        [SerializeField] private RoadChunkTile[] _tilesRC;
        [SerializeField] private RoadChunkTile _forkTileRC;
        [SerializeField] private RoadChunkTile _junctionTileRC;

        [Header("Map Fit")]
        [SerializeField, Range(0.1f, 1f)] private float _fitParentPercentage = 0.95f;
        [SerializeField] private float _rowHeight = 80f;

        [Header("Fog Overlay")]
        [SerializeField] private Sprite[] _fogSprites;
        [SerializeField] private int _fogColumns = 6;
        [SerializeField] private int _fogRows = 10;
        [SerializeField] private float _revealStepDelay = 0.15f;

        [Header("Power-Ups")]
        [SerializeField] private SOPowerUpIcons _powerUpIcons;
        [SerializeField] private SOPowerUpsData _powerUpsData;
        [SerializeField] private SOItem _itemData;

        #region Visual Elements
        private VisualElement _root;
        private TemplateContainer _raceMap;
        private TemplateContainer _brokenRaceMap;
        private VisualElement _mapTiles;
        private VisualElement _mapContent;
        private VisualElement _fogGrid;
        private VisualElement[,] _fogTiles;
        private VisualElement _powerUpArea;
        private VisualElement _leftColumn;
        private VisualElement _rightColumn;
        #endregion

        #region Private Fields
        private float _tileLeftAnchorOut;
        private float _tileRightAnchorOut;

        private List<GeneratedRoadChunkInfoWithItems> _currentSplitData = new();
        private readonly List<GeneratedItemInfo> _leftPowerUps = new();
        private readonly List<GeneratedItemInfo> _rightPowerUps = new();

        private readonly List<CachedSplitMap> _cachedSplitMaps = new();
        private CachedSplitMap? _currentCachedMap;

        private int _revealedFogSteps;
        private readonly List<Vector2Int> _leftFogRevealOrder = new();
        private readonly List<Vector2Int> _rightFogRevealOrder = new();

        private Coroutine _fitRoutine;
        private float _mapMinX;
        private float _mapMaxX;
        private int _generatedRowCount;
        private bool _needsMapRebuild;

        private bool _isShowing = false;
        private int? _teamId;
        private bool _isHitBySpear = false;
        #endregion

        #region Public Fields
        public bool IsShowing => _isShowing;
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
            {
                _teamId = _viewModel.TryGetTeamIdFromClientId(_viewModel.ClientId, out int? teamId) ? teamId : null;
                _viewModel.OnRaceTeamDataChanged += CheckPowerUpUpdateFromTeamData;


                BuildFogGrid();
                BuildFogRevealOrder();
                ResetFogRevealProgress();
            }
        }


        private void OnDestroy() //TODO: Needed?
        {
            RoadManager.OnSplitGeneratedProvided -= SplitGeneratedHandler;

            if (_fitRoutine != null)
            {
                StopCoroutine(_fitRoutine);
                _fitRoutine = null;
            }
        }

        private void OnEnable()
        {
            if (_hudDocument == null)
            {
                Log.ELazy(() => "Race Map Document reference missing!", this, _log);
                return;
            }

            _root = _hudDocument.rootVisualElement;
            _raceMap = _root.Q<TemplateContainer>("RaceMap");
            _brokenRaceMap = _root.Q<TemplateContainer>("RaceMapBroken");
            _mapTiles = _raceMap.Q<VisualElement>("MapTiles");
            _fogGrid = _raceMap.Q<VisualElement>("FogGrid");
            _powerUpArea = _raceMap.Q<VisualElement>("PowerUpArea");
            _leftColumn = _raceMap.Q<VisualElement>("LeftColumn");
            _rightColumn = _raceMap.Q<VisualElement>("RightColumn");

            if (_mapTiles == null)
            {
                Log.ELazy(() => "MapTiles not found.", this, _log);
                return;
            }

            _mapContent = new VisualElement();
            _mapContent.name = "MapContent";
            _mapContent.AddToClassList("map-content");

            _mapTiles.Clear();
            _mapTiles.Add(_mapContent);

            RoadManager.OnSplitGeneratedProvided += SplitGeneratedHandler;

            Show(false);
        }

        private void OnDisable()
        {
            RoadManager.OnSplitGeneratedProvided -= SplitGeneratedHandler;

            if (_role == RaceRole.Shotgun)
                _viewModel.OnRaceTeamDataChanged -= CheckPowerUpUpdateFromTeamData;

            if (_fitRoutine != null)
            {
                StopCoroutine(_fitRoutine);
                _fitRoutine = null;
            }
        }

        #region Handlers

        private void SplitGeneratedHandler(List<GeneratedRoadChunkInfoWithItems> splitData)
        {
            if (splitData == null)
            {
                Log.ELazy(() => "Split data is null. Cannot handle split generated event.", this, _log);
                return;
            }

            if (!TryGetSplitBounds(splitData, out int startChunkId, out int endChunkId))
            {
                Log.ELazy(() => "Cannot cache split map because bounds are invalid.", this, _log);
                return;
            }

            _cachedSplitMaps.Add(new CachedSplitMap(startChunkId, endChunkId, splitData));

            TryLoadMapForCurrentTeamProgress();
        }

        private void CheckPowerUpUpdateFromTeamData()
        {
            if (_viewModel == null)
                return;

            IReadOnlyDictionary<int, RaceTeamData> raceTeamData = new Dictionary<int, RaceTeamData>(_viewModel.NetState.TeamData);
            _isHitBySpear = raceTeamData.TryGetValue(_teamId.Value, out RaceTeamData teamData) && teamData.ActivePowerUpInfo.isTargetedBySpear;

            if (_isShowing)
                Show(true);
        }

        #endregion


        #region Race Map Construction and Update Methods

        private void BuildRaceMap()
        {
            if (_mapContent == null)
                return;

            if (_tilesRC == null || _tilesRC.Length == 0)
            {
                Log.ELazy(() => "Road chunk tile array is empty.", this, _log);
                return;
            }

            ResetMapContentTransform();
            _mapContent.Clear();
            ResetGeneratedBounds();

            if (!AddCenteredFork())
            {
                Log.ELazy(() => "Failed to add centered fork.", this, _log);
                return;
            }

            BuildRowsFromSplit();

            if (!AddCenteredJunction())
            {
                Log.ELazy(() => "Failed to add centered junction.", this, _log);
                return;
            }

            RequestFitMapToParent();
        }

        private bool AddCenteredFork()
        {
            if (_forkTileRC == null || _forkTileRC.Icon == null)
                return false;

            float forkWidth = GetSpriteWidthFromHeight(_forkTileRC.Icon, _rowHeight);

            VisualElement row = CreateRow();
            row.AddToClassList("row-fork");

            VisualElement fork = CreateChunk(_forkTileRC, forkWidth);
            fork.AddToClassList("fork");

            float forkX = (_mapTiles.resolvedStyle.width - forkWidth) * 0.5f;

            fork.style.left = forkX;

            RegisterChunkBounds(forkX, forkWidth);
            _generatedRowCount++;

            row.Add(fork);
            _mapContent.Add(row);

            _tileLeftAnchorOut = forkX + _forkTileRC.AnchorInPx;
            _tileRightAnchorOut = forkX + _forkTileRC.AnchorOutPx;

            return true;
        }

        private void BuildRowsFromSplit()
        {
            float prevAnchorLeft = _tileLeftAnchorOut;
            float prevAnchorRight = _tileRightAnchorOut;

            VisualElement currentRow = null;
            int chunksInCurrentRow = 0;

            foreach (GeneratedRoadChunkInfoWithItems data in _currentSplitData)
            {
                GeneratedRoadChunkInfo info = data.roadChunkInfo;

                if (info.position == RoadChunkPosition.MIDDLE)
                    continue;

                ExtractPowerUps(data);

                int tileIndex = GetTileIndex(info);

                if (tileIndex < 0 || tileIndex >= _tilesRC.Length)
                {
                    Log.ELazy(() => $"Invalid tile index {tileIndex} for road chunk type {info.type} at position {info.position}", this, _log);
                    continue;
                }

                if (currentRow == null)
                {
                    currentRow = CreateRow();
                    chunksInCurrentRow = 0;
                }

                if (info.position == RoadChunkPosition.LEFT)
                {
                    prevAnchorLeft = AddChunkToRow(currentRow, tileIndex, prevAnchorLeft);
                    _tileLeftAnchorOut = prevAnchorLeft;
                }
                else if (info.position == RoadChunkPosition.RIGHT)
                {
                    prevAnchorRight = AddChunkToRow(currentRow, tileIndex, prevAnchorRight);
                    _tileRightAnchorOut = prevAnchorRight;
                }

                chunksInCurrentRow++;

                if (chunksInCurrentRow == 2)
                {
                    _mapContent.Add(currentRow);
                    _generatedRowCount++;

                    currentRow = null;
                    chunksInCurrentRow = 0;
                }
            }
        }

        private bool AddCenteredJunction()
        {
            if (_junctionTileRC == null || _junctionTileRC.Icon == null)
                return false;

            float junctionWidth = GetSpriteWidthFromHeight(_junctionTileRC.Icon, _rowHeight);

            VisualElement row = CreateRow();
            row.AddToClassList("row-junction");

            VisualElement junction = CreateChunk(_junctionTileRC, junctionWidth);
            junction.AddToClassList("junction");

            float junctionX = (_mapTiles.resolvedStyle.width - junctionWidth) * 0.5f;

            junction.style.left = junctionX;

            RegisterChunkBounds(junctionX, junctionWidth);
            _generatedRowCount++;

            row.Add(junction);
            _mapContent.Add(row);

            return true;
        }

        private int GetTileIndex(GeneratedRoadChunkInfo info)
        {
            if (info.type == RoadChunkType.STRAIGHT)
                return 8;

            return info.index;
        }

        private float AddChunkToRow(VisualElement row, int tileIndex, float previousAnchorOutAbs)
        {
            RoadChunkTile tileData = _tilesRC[tileIndex];

            if (tileData == null || tileData.Icon == null)
                return previousAnchorOutAbs;

            float tileWidth = GetSpriteWidthFromHeight(tileData.Icon, _rowHeight);

            float tileX = previousAnchorOutAbs - tileData.AnchorInPx;
            float nextAnchorOutAbs = tileX + tileData.AnchorOutPx;

            VisualElement chunk = CreateChunk(tileData, tileWidth);
            chunk.style.left = tileX;

            RegisterChunkBounds(tileX, tileWidth);

            row.Add(chunk);

            return nextAnchorOutAbs;
        }

        private VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            row.style.height = _rowHeight;
            return row;
        }

        private VisualElement CreateChunk(RoadChunkTile tileData, float width)
        {
            var chunk = new VisualElement();

            chunk.AddToClassList("rc-icon");
            chunk.style.backgroundImage = new StyleBackground(tileData.Icon);
            chunk.style.width = width;
            chunk.style.height = Length.Percent(100);
            chunk.style.position = Position.Absolute;

            return chunk;
        }

        private float GetSpriteWidthFromHeight(Sprite sprite, float height)
        {
            if (sprite == null || sprite.rect.height <= 0f)
                return height;

            return height * (sprite.rect.width / sprite.rect.height);
        }

        private void ClearRaceMap()
        {
            _mapContent?.Clear();
        }

        #endregion

        #region Map Fit Methods

        private void RequestFitMapToParent()
        {
            if (_fitRoutine != null)
                StopCoroutine(_fitRoutine);

            _fitRoutine = StartCoroutine(FitMapToParentNextFrame());
        }

        private IEnumerator FitMapToParentNextFrame()
        {
            ResetMapContentTransform();

            yield return null;

            FitMapToParent();

            _fitRoutine = null;
        }

        private void ResetMapContentTransform()
        {
            if (_mapContent == null)
                return;

            _mapContent.style.scale = new Scale(Vector2.one);
            _mapContent.style.translate = new Translate(0, 0);
            _mapContent.style.transformOrigin = new TransformOrigin(
                Length.Percent(50),
                Length.Percent(50),
                0
            );
        }

        private void FitMapToParent()
        {
            if (_mapTiles == null || _mapContent == null)
                return;

            float containerWidth = _mapTiles.resolvedStyle.width;
            float containerHeight = _mapTiles.resolvedStyle.height;

            if (float.IsNaN(containerWidth) || containerWidth <= 0f)
                return;

            if (float.IsNaN(containerHeight) || containerHeight <= 0f)
                return;

            if (_mapMinX == float.MaxValue)
                return;

            float mapHeight = _generatedRowCount * _rowHeight;

            if (mapHeight <= 0f)
                return;

            float centerX = containerWidth * 0.5f;

            float maxHorizontalExtent = Mathf.Max(
                Mathf.Abs(centerX - _mapMinX),
                Mathf.Abs(_mapMaxX - centerX)
            );

            if (maxHorizontalExtent <= 0f)
                return;

            float targetHalfWidth = containerWidth * _fitParentPercentage * 0.5f;
            float targetHeight = containerHeight * _fitParentPercentage;

            float widthScale = targetHalfWidth / maxHorizontalExtent;
            float heightScale = targetHeight / mapHeight;

            float finalScale = Mathf.Min(widthScale, heightScale);

            _mapContent.style.scale = new Scale(new Vector2(finalScale, finalScale));

            Log.DLazy(() =>
                $"Fit | minX: {_mapMinX} | maxX: {_mapMaxX} | height: {mapHeight} | scale: {finalScale}",
                this, _log
            );
        }

        private void ResetGeneratedBounds()
        {
            _mapMinX = float.MaxValue;
            _mapMaxX = float.MinValue;
            _generatedRowCount = 0;
        }

        private void RegisterChunkBounds(float x, float width)
        {
            _mapMinX = Mathf.Min(_mapMinX, x);
            _mapMaxX = Mathf.Max(_mapMaxX, x + width);
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
            if (!progress.LastSpecialChunkType.HasValue)
                return null;

            for (int i = 0; i < _cachedSplitMaps.Count; i++)
            {
                CachedSplitMap map = _cachedSplitMaps[i];

                if ((progress.LastSpecialChunkType.Value.Type == RoadChunkType.START_LINE ||
                     progress.LastSpecialChunkType.Value.Type == RoadChunkType.ENDING_CROSSROAD) &&
                    progress.NextSpecialChunkId == map.StartChunkId)
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

            if (!IsMapLayoutReady())
            {
                _needsMapRebuild = true;
                return;
            }

            RebuildCurrentMap();
        }

        private bool IsMapLayoutReady()
        {
            if (_mapTiles == null)
                return false;

            float width = _mapTiles.resolvedStyle.width;
            float height = _mapTiles.resolvedStyle.height;

            return !float.IsNaN(width) && width > 0f &&
                   !float.IsNaN(height) && height > 0f;
        }

        private void RebuildCurrentMap()
        {
            _needsMapRebuild = false;

            ResetFogRevealProgress();
            BuildFogRevealOrder();

            ClearRaceMap();
            ClearPowerUps();

            BuildRaceMap();
            PopulatePowerUps();
        }



        private IEnumerator RefreshMapAfterShow()
        {
            yield return null;

            if (!IsMapLayoutReady())
                yield break;

            if (_needsMapRebuild && _currentCachedMap.HasValue)
            {
                RebuildCurrentMap();
                yield break;
            }

            RequestFitMapToParent();
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
                    VisualElement tile = _fogTiles[x, y];

                    if (tile == null)
                        continue;

                    tile.RemoveFromClassList("fog-fade");
                    tile.RemoveFromClassList("revealed");
                    tile.style.opacity = 1f;
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

            VisualElement tile = _fogTiles[x, y];

            if (tile == null)
                return;

            tile.style.opacity = StyleKeyword.Null;
            tile.AddToClassList("fog-fade");
            tile.AddToClassList("revealed");
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
            if (column == null)
                return;

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
            // PowerUp powerUp = _itemData.PowerUpItems[item.index].PowerUpType;

            return _powerUpIcons.GetIcon(powerUp);
        }

        #endregion

        #region Public Methods

        public void Show(bool show)
        {
            _raceMap.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
            _isShowing = show;

            if (!show)
            {
                _brokenRaceMap.style.visibility = Visibility.Hidden;
                return;
            }

            if (!_isHitBySpear)
            {
                _brokenRaceMap.style.visibility = Visibility.Hidden;
                StartCoroutine(RefreshMapAfterShow());
            }
            else
            {
                _raceMap.style.visibility = Visibility.Hidden;
                _brokenRaceMap.style.visibility = Visibility.Visible;
            }
        }


        public void UpdateMapFromTeamProgress(TeamTrackProgress progress)
        {
            TryLoadMapForProgress(progress);
            UpdateFogFromProgress(progress);
        }

        #endregion
    }
}
