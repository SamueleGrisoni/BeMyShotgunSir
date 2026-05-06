using BeMyShotgunSir.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class RaceMapController : MonoBehaviour
{
    [SerializeField] private UIDocument _raceMapDocument;
    [SerializeField] private Tile_RoadChunk[] _tilesRC;
    [SerializeField] private Tile_RoadChunk _forkTileRC;
    [SerializeField] private Tile_RoadChunk _junctionTileRC;

    [SerializeField] private int _maxLenght;
    private int _tileLeftAnchorOut;
    private int _tileRightAnchorOut;

    private VisualElement _root;
    private VisualElement _mapTiles;

    private void OnEnable()
    {
        if (_raceMapDocument == null)
        {
            Debug.Log("Race Map Document reference missing!");
            return;
        }

        _root = _raceMapDocument.rootVisualElement;
        _mapTiles = _root.Q<VisualElement>("MapTiles");

        BuildRaceMap();
    }

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

        for (int i = 0; i < _maxLenght; i++)
        {
            _mapTiles.Add(BuildRow(_tileLeftAnchorOut, _tileRightAnchorOut));
        }

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

    private VisualElement BuildRow(int prevAnchorLeft = 0, int prevAnchorRight = 0)
    {
        int left = Random.Range(0, _tilesRC.Length);
        int right = Random.Range(0, _tilesRC.Length);

        int leftAnchorIn = _tilesRC[left].AnchorInPx + prevAnchorLeft;
        int rightAnchorIn = _tilesRC[right].AnchorInPx + prevAnchorRight;

        var chunkLeft = new VisualElement();
        chunkLeft.AddToClassList("rc-icon");
        chunkLeft.style.backgroundImage = new StyleBackground(_tilesRC[left].Icon);
        chunkLeft.style.left = leftAnchorIn;


        var chunkRight = new VisualElement();
        chunkRight.AddToClassList("rc-icon");
        chunkRight.style.backgroundImage = new StyleBackground(_tilesRC[right].Icon);
        chunkRight.style.left = rightAnchorIn;


        var row = new VisualElement();
        row.AddToClassList("row");
        row.Add(chunkLeft);
        row.Add(chunkRight);

        _tileLeftAnchorOut = _tilesRC[left].AnchorOutPx + prevAnchorLeft;
        _tileRightAnchorOut = _tilesRC[right].AnchorOutPx + prevAnchorRight;

        return row;
    }

}
