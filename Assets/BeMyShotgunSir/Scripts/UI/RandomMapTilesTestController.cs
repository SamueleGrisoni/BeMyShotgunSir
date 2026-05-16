using System.Collections;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

public class RandomMapTilesTestController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    #region Debug
    [Header("Debug Anchor Test")]
    [SerializeField] private bool _debugAnchorTestEnabled;

    [SerializeField] private RoadChunkTile _debugForkTile;
    [SerializeField] private RoadChunkTile _debugTile;
    [SerializeField] private RoadChunkTile _debugNextTile;

    [SerializeField] private float _debugForkAnchorLeft;
    [SerializeField] private float _debugForkAnchorRight;

    [SerializeField] private bool _debugUseRightForkAnchor;

    [SerializeField] private float _debugTileAnchorIn;
    [SerializeField] private float _debugTileAnchorOut;

    [SerializeField] private float _debugRowHeight = 80f;

    private bool _debugAnchorTestBuilt;
    private VisualElement _debugTileElement;
    private VisualElement _debugNextTileElement;

    private float _lastDebugForkAnchorLeft;
    private float _lastDebugForkAnchorRight;
    private float _lastDebugTileAnchorIn;
    private float _lastDebugTileAnchorOut;
    private float _lastDebugNextTileAnchorIn;
    private float _lastDebugNextTileAnchorOut;
    private bool _lastDebugUseRightForkAnchor;

    #endregion

    [Header("Fit")]
    [SerializeField, Range(0.1f, 1f)] private float _fitParentPercentage = 0.95f;

    [Header("Tiles")]
    [SerializeField] private RoadChunkTile[] _tiles;
    [SerializeField] private RoadChunkTile _forkTile;
    [SerializeField] private RoadChunkTile _junctionTile;

    [Header("Generation")]
    [SerializeField] private int _rows = 12;
    [SerializeField] private float _rowHeight = 80f;

    private VisualElement _root;
    private VisualElement _mapTiles;
    private VisualElement _mapContent;

    private float _leftAnchorOutAbs;
    private float _rightAnchorOutAbs;
    private Coroutine _fitRoutine;
    private float _mapMinX;
    private float _mapMaxX;
    private int _generatedRowCount;


    private void OnEnable()
    {
        if (_document == null)
        {
            Debug.LogError("UIDocument missing.");
            return;
        }

        _root = _document.rootVisualElement;
        _mapTiles = _root.Q<VisualElement>("MapTiles");

        if (_mapTiles == null)
        {
            Debug.LogError("MapTiles not found.");
            return;
        }

        _mapContent = new VisualElement();
        _mapContent.name = "MapContent";
        _mapContent.AddToClassList("map-content");

        _mapTiles.Clear();
        _mapTiles.Add(_mapContent);

        _mapTiles.RegisterCallback<GeometryChangedEvent>(OnMapTilesGeometryChanged);
    }

    private void OnDisable()
    {
        _mapTiles?.UnregisterCallback<GeometryChangedEvent>(OnMapTilesGeometryChanged);
        if (_fitRoutine != null)
        {
            StopCoroutine(_fitRoutine);
            _fitRoutine = null;
        }
    }

    private void Update()
    {
        UpdateDebugAnchorTest();
    }

    private void OnMapTilesGeometryChanged(GeometryChangedEvent evt)
    {
        if (float.IsNaN(evt.newRect.width) || evt.newRect.width <= 0)
            return;

        _mapTiles.UnregisterCallback<GeometryChangedEvent>(OnMapTilesGeometryChanged);

        Generate();
    }

    [ContextMenu("Generate Random Map")]
    private void Generate()
    {
        if (_mapContent == null)
            return;

        if (_tiles == null || _tiles.Length == 0)
        {
            Log.ELazy(() => "Tiles array is empty.", this);
            return;
        }

        ResetMapContentTransform();
        _mapContent.Clear();
        ResetGeneratedBounds();

        if (!AddCenteredFork())
        {
            Log.ELazy(() => "Failed to add centered fork.", this);
            return;
        }

        for (int i = 0; i < _rows; i++)
        {
            VisualElement row = BuildRandomRow();
            _mapContent.Add(row);
        }

        if (!AddCenteredJunction())
        {
            Log.ELazy(() => "Failed to add centered junction.", this);
            return;
        }

        RequestFitMapToParent();
    }

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

    private bool AddCenteredFork()
    {
        if (_forkTile == null || _forkTile.Icon == null)
            return false;

        float forkWidth = GetSpriteWidthFromHeight(_forkTile.Icon, _rowHeight);

        VisualElement row = CreateRow();

        VisualElement fork = CreateChunk(_forkTile, forkWidth);
        fork.AddToClassList("fork");

        float forkX = (_mapTiles.resolvedStyle.width - forkWidth) * 0.5f;

        RegisterChunkBounds(forkX, forkWidth);
        _generatedRowCount++;

        fork.style.left = forkX;

        row.Add(fork);
        _mapContent.Add(row);

        _leftAnchorOutAbs = forkX + _forkTile.AnchorInPx;
        _rightAnchorOutAbs = forkX + _forkTile.AnchorOutPx;

        return true;
    }

    private VisualElement BuildRandomRow()
    {
        VisualElement row = CreateRow();

        AddBranchChunk(
            row,
            _tiles[Random.Range(0, _tiles.Length)],
            ref _leftAnchorOutAbs);

        AddBranchChunk(
            row,
            _tiles[Random.Range(0, _tiles.Length)],
            ref _rightAnchorOutAbs);

        _generatedRowCount++;

        return row;
    }

    private void AddBranchChunk(
        VisualElement row,
        RoadChunkTile tileData,
        ref float previousAnchorOutAbs
    )
    {
        if (tileData == null || tileData.Icon == null)
            return;

        float tileWidth = GetSpriteWidthFromHeight(tileData.Icon, _rowHeight);

        float tileX = previousAnchorOutAbs - tileData.AnchorInPx;
        float nextAnchorOutAbs = tileX + tileData.AnchorOutPx;

        VisualElement chunk = CreateChunk(tileData, tileWidth);
        chunk.style.left = tileX;

        RegisterChunkBounds(tileX, tileWidth);

        row.Add(chunk);

        previousAnchorOutAbs = nextAnchorOutAbs;
    }

    private bool AddCenteredJunction()
    {
        if (_junctionTile == null || _junctionTile.Icon == null)
            return false;

        float junctionWidth = GetSpriteWidthFromHeight(_junctionTile.Icon, _rowHeight);

        VisualElement row = CreateRow();

        VisualElement junction = CreateChunk(_junctionTile, junctionWidth);
        junction.AddToClassList("junction");

        float mapWidth = _mapTiles.resolvedStyle.width;
        float junctionX = mapWidth * 0.5f - junctionWidth * 0.5f;

        junction.style.left = junctionX;

        RegisterChunkBounds(junctionX, junctionWidth);
        _generatedRowCount++;

        row.Add(junction);
        _mapContent.Add(row);
        return true;
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

    private void ResetMapContentTransform()
    {
        if (_mapContent == null)
            return;

        _mapContent.style.scale = new Scale(Vector2.one);
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
            this
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












    #region Debug Anchor Test

    private void UpdateDebugAnchorTest()
    {
        if (!_debugAnchorTestEnabled)
        {
            if (_debugAnchorTestBuilt)
                ClearDebugAnchorTest();

            return;
        }

        if (!_debugAnchorTestBuilt)
        {
            BuildDebugAnchorTest();
            return;
        }

        if (_debugTileElement == null)
            return;

        if (Mathf.Approximately(_debugForkAnchorLeft, _lastDebugForkAnchorLeft) &&
            Mathf.Approximately(_debugForkAnchorRight, _lastDebugForkAnchorRight) &&
            Mathf.Approximately(_debugTileAnchorIn, _lastDebugTileAnchorIn) &&
            Mathf.Approximately(_debugTileAnchorOut, _lastDebugTileAnchorOut) &&
            _debugNextTile != null &&
            Mathf.Approximately(_debugNextTile.AnchorInPx, _lastDebugNextTileAnchorIn) &&
            Mathf.Approximately(_debugNextTile.AnchorOutPx, _lastDebugNextTileAnchorOut) &&
            _debugUseRightForkAnchor == _lastDebugUseRightForkAnchor)
            return;

        ApplyDebugAnchors();
    }

    private void BuildDebugAnchorTest()
    {
        if (_mapContent == null || _debugForkTile == null || _debugTile == null)
            return;


        if (float.IsNaN(_mapTiles.resolvedStyle.width) || _mapTiles.resolvedStyle.width <= 0f)
            return;

        ResetMapContentTransform();
        _mapContent.Clear();

        VisualElement forkRow = CreateDebugRow();
        VisualElement tileRow = CreateDebugRow();
        VisualElement nextTileRow = CreateDebugRow();

        float forkWidth = GetSpriteWidthFromHeight(_debugForkTile.Icon, _debugRowHeight);

        VisualElement forkElement = CreateChunk(_debugForkTile, forkWidth);
        forkElement.AddToClassList("fork");

        float forkX = (_mapTiles.resolvedStyle.width - forkWidth) * 0.5f;
        forkElement.style.left = forkX;

        forkRow.Add(forkElement);
        _mapContent.Add(forkRow);

        float tileWidth = GetSpriteWidthFromHeight(_debugTile.Icon, _debugRowHeight);

        _debugTileElement = CreateChunk(_debugTile, tileWidth);

        tileRow.Add(_debugTileElement);
        _mapContent.Add(tileRow);

        if (_debugNextTile != null && _debugNextTile.Icon != null)
        {
            float nextTileWidth = GetSpriteWidthFromHeight(_debugNextTile.Icon, _debugRowHeight);

            _debugNextTileElement = CreateChunk(_debugNextTile, nextTileWidth);

            nextTileRow.Add(_debugNextTileElement);
            _mapContent.Add(nextTileRow);
        }

        _debugAnchorTestBuilt = true;

        ForceDebugRefresh();
        ApplyDebugAnchors();
    }

    private void ApplyDebugAnchors()
    {
        if (_debugTileElement == null || _debugForkTile == null)
            return;

        float forkWidth = GetSpriteWidthFromHeight(_debugForkTile.Icon, _debugRowHeight);

        float forkX = (_mapTiles.resolvedStyle.width - forkWidth) * 0.5f;

        float selectedForkAnchor = _debugUseRightForkAnchor
            ? _debugForkAnchorRight
            : _debugForkAnchorLeft;

        float previousAnchorOutAbs = forkX + selectedForkAnchor;

        float tileX = previousAnchorOutAbs - _debugTileAnchorIn;
        float nextAnchorOutAbs = tileX + _debugTileAnchorOut;

        _debugTileElement.style.left = tileX;

        if (_debugNextTileElement != null && _debugNextTile != null)
        {
            float nextTileX = nextAnchorOutAbs - _debugNextTile.AnchorInPx;
            float nextNextAnchorOutAbs = nextTileX + _debugNextTile.AnchorOutPx;

            _debugNextTileElement.style.left = nextTileX;

            _lastDebugNextTileAnchorIn = _debugNextTile.AnchorInPx;
            _lastDebugNextTileAnchorOut = _debugNextTile.AnchorOutPx;

            Log.DLazy(() =>
                $"Anchor test | ForkX: {forkX} | SelectedForkAnchorAbs: {previousAnchorOutAbs} | " +
                $"TileIn: {_debugTileAnchorIn} | TileOut: {_debugTileAnchorOut} | TileX: {tileX} | NextOutAbs: {nextAnchorOutAbs} | " +
                $"NextTileIn: {_debugNextTile.AnchorInPx} | NextTileOut: {_debugNextTile.AnchorOutPx} | NextTileX: {nextTileX} | NextNextOutAbs: {nextNextAnchorOutAbs}", this
            );
        }
        else
        {
            Log.DLazy(() =>
                $"Anchor test | ForkX: {forkX} | SelectedForkAnchorAbs: {previousAnchorOutAbs} | " +
                $"TileIn: {_debugTileAnchorIn} | TileOut: {_debugTileAnchorOut} | TileX: {tileX} | NextOutAbs: {nextAnchorOutAbs}", this
            );
        }

        _lastDebugForkAnchorLeft = _debugForkAnchorLeft;
        _lastDebugForkAnchorRight = _debugForkAnchorRight;
        _lastDebugTileAnchorIn = _debugTileAnchorIn;
        _lastDebugTileAnchorOut = _debugTileAnchorOut;
        _lastDebugUseRightForkAnchor = _debugUseRightForkAnchor;
    }

    private void ClearDebugAnchorTest()
    {
        _mapContent?.Clear();

        _debugTileElement = null;
        _debugNextTileElement = null;
        _debugAnchorTestBuilt = false;

        ResetMapContentTransform();
    }

    private void ForceDebugRefresh()
    {
        _lastDebugForkAnchorLeft = float.NaN;
        _lastDebugForkAnchorRight = float.NaN;
        _lastDebugTileAnchorIn = float.NaN;
        _lastDebugTileAnchorOut = float.NaN;
        _lastDebugNextTileAnchorIn = float.NaN;
        _lastDebugNextTileAnchorOut = float.NaN;
    }

    private VisualElement CreateDebugRow()
    {
        var row = new VisualElement();
        row.AddToClassList("row");
        row.style.height = _debugRowHeight;
        return row;
    }


    #endregion
}
