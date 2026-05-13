using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track;
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

        [SerializeField] private int _maxLenght;

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
        #endregion

        #region private fields
        private int _tileLeftAnchorOut;
        private int _tileRightAnchorOut;
        private List<GeneratedRoadChunkInfoWithItems> _currentSplitData = new List<GeneratedRoadChunkInfoWithItems>();
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
            BuildRaceMap();
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

        public void Show(bool show)
        {
            _raceMap.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _isShowing = show;
        }
    }
}
