using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GridFactory : MonoBehaviour
{
    [SerializeField] private CellPlace _prefab;
    [SerializeField] private Vector2Int _size;
    [SerializeField] private CellView[] _cellTypes;

    private RectTransform _rectTransform;
    private GridModel _model;
    private CoordinateGrid _coordinateGrid;

    public GridModel RelevantModel => _model;
    public Vector2Int Size => _size;
    public CellView[] CellTypes => _cellTypes;

    public void Init()
    {
        _rectTransform = GetComponent<RectTransform>();
        _model = new GridModel(_size, _cellTypes.Length, 3);

        Vector2 cellSize = _prefab.GetComponent<RectTransform>().sizeDelta;
        _rectTransform.sizeDelta = _size * cellSize;
        Vector2 position = (-_rectTransform.sizeDelta + cellSize) / 2;
        _coordinateGrid = new CoordinateGrid(cellSize.x, position);
    }

    public IEnumerable<(CellPlace, Vector2Int)> FillPlaces()
    {
        Queue<CellPlace> cellPlaces = new Queue<CellPlace>();

        for (int rows = 0; rows < _size.y; rows++)
        {
            for (int columns = 0; columns < _size.x; columns++)
            {
                CellPlace currentCell = Instantiate(_prefab, transform);
                currentCell.transform.localPosition = _coordinateGrid[columns, rows];
                cellPlaces.Enqueue(currentCell);

                yield return (currentCell, new Vector2Int(columns, rows));
            }
        }

        for (int rows = 0; rows < _size.y; rows++)
        {
            for (int columns = 0; columns < _size.x; columns++)
            {
                cellPlaces.Dequeue().SetCell(CreateRandomElement(columns, rows));
            }
        }
    }

    public CellView CreateRandomElement(int x, int y)
    {
        Vector3 spawnPosition = _coordinateGrid[x, _size.y + y];
        CellView prefab = _cellTypes[_model[x, y].Value];
        CellView currentCell = Instantiate(prefab, transform);
        currentCell.transform.localPosition = spawnPosition;

        return currentCell;
    }

    public CellView CreateRandomElement(Vector2Int coordinates, int spawnHeightIndex)
    {
        Vector3 spawnPosition = _coordinateGrid[coordinates.x, coordinates.y + spawnHeightIndex];
        CellView prefab = _cellTypes[_model[coordinates.x, coordinates.y].Value];
        CellView currentCell = Instantiate(prefab, transform);
        currentCell.transform.localPosition = spawnPosition;

        return currentCell;
    }
}