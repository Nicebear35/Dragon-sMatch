using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class GridView : MonoBehaviour
{
    private CellPlace[,] _grid;
    private GridFactory _factory;
    private GridModel _model;
    private AnimationObserver _observer;

    private Dictionary<CellPlace, Vector2Int> _cells;

    private CellPlace _selectedCell;

    private void Awake()
    {
        _factory = GetComponent<GridFactory>();
        _factory.Init();
        _grid = new CellPlace[_factory.Size.x, _factory.Size.y];
        _cells = new Dictionary<CellPlace, Vector2Int>();
        _model = _factory.RelevantModel;
        _observer = new AnimationObserver();

        foreach (var tuple in _factory.FillPlaces())
        {
            CellPlace cell = tuple.Item1;
            Vector2Int coordinates = tuple.Item2;

            _cells.Add(cell, coordinates);
            _grid[coordinates.x, coordinates.y] = cell;
        }
    }

    private void OnEnable()
    {
        foreach (var cell in _grid)
        {
            cell.Selected += OnCellPlaceSelected;
            cell.Deselected += OnCellPlaceDeselected;
        }

        _model.CellCreated += FallDown;
        _model.CellFallenDown += FallDown;
        _observer.AllAnimationsEnded += ProcessAdditionalCombinations;
    }

    private void OnDisable()
    {
        foreach (var cell in _grid)
        {
            cell.Selected -= OnCellPlaceSelected;
            cell.Deselected -= OnCellPlaceDeselected;
        }

        _model.CellCreated -= FallDown;
        _model.CellFallenDown -= FallDown;
        _observer.AllAnimationsEnded -= ProcessAdditionalCombinations;
    }

    private void OnCellPlaceSelected(CellPlace selectedCell)
    {
        if (_selectedCell == null)
        {
            _selectedCell = selectedCell;
            return;
        }

        if (AreNeighbours(_selectedCell, selectedCell))
        {
            CellPlace firstCell = _selectedCell;
            CellView tempCell = _selectedCell.Cell;

            Tween moving = _selectedCell.SetCell(selectedCell.Cell);
            selectedCell.SetCell(tempCell);

            _selectedCell.Deselect();
            selectedCell.Deselect();

            moving.onComplete += () => ProcessCellSelection(firstCell, selectedCell);
        }
        else
        {
            _selectedCell.Deselect();
            _selectedCell = selectedCell;
        }
    }

    private void OnCellPlaceDeselected(CellPlace selectedCell)
    {
        if (_selectedCell == selectedCell)
        {
            _selectedCell = null;
        }
    }

    private bool AreNeighbours(CellPlace first, CellPlace second)
    {
        Vector2Int firstCoordinates = _cells[first];
        Vector2Int secondCoordinates = _cells[second];

        return (firstCoordinates - secondCoordinates).sqrMagnitude == 1;
    }

    private bool TrySwapCells(CellPlace first, CellPlace second, out IEnumerable<Vector2Int> cellsToDelete)
    {
        bool success = _model.TrySwapCells(_cells[first], _cells[second], out cellsToDelete);

        if (!success)
        {
            CellView tempCell = first.Cell;

            first.SetCell(second.Cell);
            second.SetCell(tempCell);
        }

        return success;
    }

    private void ProcessCellSelection(CellPlace first, CellPlace second)
    {
        if (TrySwapCells(first, second, out IEnumerable<Vector2Int> cellsToDelete))
        {
            foreach (var coordinate in cellsToDelete)
            {
                _grid[coordinate.x, coordinate.y].DeleteElement();
            }

            _model.FallDown(cellsToDelete);
        }
    }

    private void FallDown(Vector2Int from, Vector2Int to)
    {
        Tween tween = _grid[to.x, to.y].SetCell(_grid[from.x, from.y].Cell);
        _observer.ObserveTweenProgress(tween);
    }

    private void FallDown(Vector2Int to, int spawnHeightIndex)
    {
        CellView currentCell = _factory.CreateRandomElement(to, spawnHeightIndex);
        Tween tween = _grid[to.x, to.y].SetCell(currentCell);
        _observer.ObserveTweenProgress(tween);
    }

    private void ProcessAdditionalCombinations()
    {

        if (_model.SearchAdditionalPaths(out IEnumerable<Vector2Int> coordinatesToDelete)) 
        {
            foreach (var coordinate in coordinatesToDelete)
            {
                _grid[coordinate.x, coordinate.y].DeleteElement();
            }

            _model.FallDown(coordinatesToDelete);
        }
    }
}
