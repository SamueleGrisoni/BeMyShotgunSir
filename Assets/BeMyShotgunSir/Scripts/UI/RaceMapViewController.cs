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
        private Coroutine _fogRevealCoroutine;


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
            StartCoroutine(TimedRandomFogRevealRoutine());

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

            _currentSplitData = splitData;
            ClearRaceMap();
            ClearPowerUps();

            BuildRaceMap();
            PopulatePowerUps();
        }
        #endregion

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

        private IEnumerator TimedRandomFogRevealRoutine()
        {
            int halfColumns = _fogColumns / 2;

            List<Vector2Int> leftTiles = new();
            List<Vector2Int> rightTiles = new();

            for (int y = 0; y < _fogRows; y++)
            {
                for (int x = 0; x < _fogColumns; x++)
                {
                    if (x < halfColumns)
                        leftTiles.Add(new Vector2Int(x, y));
                    else
                        rightTiles.Add(new Vector2Int(x, y));
                }
            }

            Shuffle(leftTiles);
            Shuffle(rightTiles);

            int steps = Mathf.Max(leftTiles.Count, rightTiles.Count);

            for (int i = 0; i < steps; i++)
            {
                if (i < leftTiles.Count)
                    RevealFogTile(leftTiles[i].x, leftTiles[i].y);

                if (i < rightTiles.Count)
                    RevealFogTile(rightTiles[i].x, rightTiles[i].y);

                yield return new WaitForSeconds(_revealStepDelay);
            }

            _fogRevealCoroutine = null;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

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


        #region Public Methods
        public void Show(bool show)
        {
            _raceMap.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _isShowing = show;
        }

        #endregion
    }
}
