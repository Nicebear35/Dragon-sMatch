using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.Events;

public class GridModel
{
    private readonly CellModel[,] _grid;

    public readonly int TargetCoincidenceLength;

    private readonly int _cellTypeCount;
    private int RandomCellValue => Random.Range(0, _cellTypeCount);
    private int RandomEvenIndex => Random.Range(0, _cellTypeCount / 2 + (_cellTypeCount % 2)) * 2;
    private int RandomOddIndex => Random.Range(0, _cellTypeCount / 2) * 2 + 1;

    private Queue<CellModel> _cellsToSearchFrom = new Queue<CellModel>();

    public CellModel this[int x, int y] => _grid[x, y];

    public event UnityAction<Vector2Int, Vector2Int> CellFallenDown;
    public event UnityAction<Vector2Int, int> CellCreated;

    public GridModel(Vector2Int size, int cellTypeCount, int targetCoincidenceLength)
    {
        _grid = new CellModel[size.x, size.y];
        _cellTypeCount = cellTypeCount;

        int id = 0;

        for (int rows = 0; rows < size.y; rows++)
        {
            for (int columns = 0; columns < size.x; columns++)
            {
                if ((rows + columns) % 2 == 0)
                {
                    _grid[columns, rows] = new CellModel(RandomEvenIndex, columns, rows, id++);
                }
                else
                {
                    _grid[columns, rows] = new CellModel(RandomOddIndex, columns, rows, id++);
                }
            }
        }

        TargetCoincidenceLength = targetCoincidenceLength;
    }


    private IEnumerable<CellModel> FindShape(CellModel start, int targetMatchValue)
    {
        HashSet<CellModel> passedCells = new HashSet<CellModel>();

        IEnumerable<Path> rightPath = FindComplicatedPath(start, Vector2Int.right, passedCells);
        IEnumerable<Path> leftPath = FindComplicatedPath(start, Vector2Int.left, passedCells);
        IEnumerable<Path> upPath = FindComplicatedPath(start, Vector2Int.up, passedCells);
        IEnumerable<Path> downPath = FindComplicatedPath(start, Vector2Int.down, passedCells);

        IEnumerable<Path> allPaths = rightPath.Concat(leftPath).Concat(upPath).Concat(downPath).ToList();

        return allPaths.SelectPathsToDelete(targetMatchValue).SelectMany(path => path).Distinct();
    }

    private IEnumerable<Path> FindComplicatedPath(CellModel start, Vector2Int direction, HashSet<CellModel> passedCells = null)
    {
        if (passedCells == null)
        {
            passedCells = new HashSet<CellModel>();
        }

        passedCells.Add(start);
        Path current = FindPath(start, direction);

        if (current.Length <= 1)
        {
            yield break;
        }

        yield return current; // это что такое? втора€ €чейка всегда возвращаетс€, если их 2 р€дом с одинаковыми значени€ми
        var filteredPath = current.Where(currentCell => passedCells.Add(currentCell)).ToArray();

        foreach (var cell in filteredPath)
        {
            Vector2Int newDirection = new Vector2Int(direction.y, direction.x);

            foreach (var path in FindComplicatedPath(cell, newDirection, passedCells))
            {
                yield return path;
            }

            foreach (var path in FindComplicatedPath(cell, -newDirection, passedCells))
            {
                yield return path;
            }
        }
    }

    private Path FindPath(CellModel start, Vector2Int direction)
    {
        Path path = new Path(start, direction);

        int counter = 0; //дл€ дебага

        while (TryMove(start, direction, out CellModel result) && start.Value == result.Value)
        {
            counter++; // дл€ дебага

            start = result;
            path.Add(result);
        }

        return path;
    }

    private bool TryMove(CellModel current, Vector2Int direction, out CellModel result)
    {
        Vector2Int next = current.Coordinates + direction;

        if (next.x < 0 || next.x >= _grid.GetLength(0) || next.y < 0 || next.y >= _grid.GetLength(1))
        {
            result = current;
            return false;
        }

        result = _grid[next.x, next.y];
        return true;
    }

    public void FallDown(IEnumerable<Vector2Int> finalShape)
    {
        var groupsToDelete = finalShape.Select(coordinate => _grid[coordinate.x, coordinate.y]).GroupBy(column => column.Coordinates.x).ToList();

        foreach (var group in groupsToDelete)
        {
            int deletedCellsAmount = 0;
            int bottom = group.Min(cell => cell.Coordinates.y);

            foreach (var subgroup in DivideIntoGroups(group))
            {
                int amountToDelete = group.Intersect(subgroup).Count() - deletedCellsAmount;

                MoveCellsInColumn(amountToDelete, subgroup);
                deletedCellsAmount += amountToDelete;
            }

            for (int i = bottom; i < _grid.GetLength(1); i++)
            {
                _cellsToSearchFrom.Enqueue(_grid[group.Key, i]);
            }
        }
    }

    private IEnumerable<ColumnPart> DivideIntoGroups(IGrouping<int, CellModel> group)
    {
        var cells = group.OrderByDescending(cell => cell.Coordinates.y).ToArray();

        Debug.Log($"ячейки на удаление: {cells.Length} / LastIndex: {cells.Length - 2}");

        foreach (var cell in cells)
        {
            Debug.Log($"координаты: {cell.Coordinates}");
        }

        int lastIndex = cells.Length - 2;
        int last = _grid.GetLength(1) - 1;

        if (cells.Length == 1)
        {
            yield return new ColumnPart(this, group.Key, cells[0].Coordinates.y, last);
        }

        for (int i = 0; i <= lastIndex; i++)
        {
            Debug.Log("мы в цикле");

            if (cells[i].Coordinates.y != cells[i + 1].Coordinates.y + 1)
            {

                Debug.Log($"cells[i]: {cells[i].Coordinates} / cells[i + 1]: {cells[i + 1].Coordinates} / i: {i}");

                CellModel start = cells[i];
                Debug.Log($"X группы: {group.Key} / начало: {start.Coordinates} / верхний Y: {last}");
                yield return new ColumnPart(this, group.Key, start.Coordinates.y, last);

                //last = cells[i].Coordinates.y - 1;
            }
            if (i == lastIndex)
            {
                CellModel start = cells[i + 1];
                yield return new ColumnPart(this, group.Key, start.Coordinates.y, last);
            }
        }
    }

    private void MoveCellsInColumn(int amountToDelete, ColumnPart column)
    {
        int fallsCount = column.Count - amountToDelete;

        Debug.Log($"fallsCount: {fallsCount}");

        for (int i = 0; i < fallsCount; i++)
        {
            Debug.Log($"в €чейку {column[i].Coordinates} падает {column[i + amountToDelete].Coordinates}");
            column[i].SetValue(column[i + amountToDelete].Value); 
            CellFallenDown?.Invoke(column[i + amountToDelete].Coordinates, column[i].Coordinates);
        }

        for (int i = 0; i < amountToDelete; i++)
        {
            int random = RandomCellValue;
            Debug.Log($"в €чейке {column[i + fallsCount].Coordinates} спавнитс€ значение {random}");
            column[i + fallsCount].SetValue(random);
            CellCreated?.Invoke(column[i + fallsCount].Coordinates, amountToDelete + column[i+fallsCount].Coordinates.y);
        }
    }

    public bool TrySwapCells(Vector2Int first, Vector2Int second, out IEnumerable<Vector2Int> cellsToDelete)
    {
        CellModel firstCell = _grid[first.x, first.y];
        CellModel secondCell = _grid[second.x, second.y];

        SwapCells(first, second);

        var firstShape = FindShape(firstCell, TargetCoincidenceLength);

        var secondShape = FindShape(secondCell, TargetCoincidenceLength);


        var finalShape = firstShape.Concat(secondShape).Distinct().ToList();
        cellsToDelete = finalShape.Select(cell => cell.Coordinates);

        if (finalShape.Count == 0)
        {
            SwapCells(first, second);
        }

        return finalShape.Count > 0;
    }

    private void SwapCells(Vector2Int firstCell, Vector2Int secondCell)
    {
        int firstValue = _grid[firstCell.x, firstCell.y].Value;
        int secondValue = _grid[secondCell.x, secondCell.y].Value;

        _grid[firstCell.x, firstCell.y].SetValue(secondValue);
        _grid[secondCell.x, secondCell.y].SetValue(firstValue);
    }

    public bool SearchAdditionalPaths(out IEnumerable<Vector2Int> coordinatesToDelete)
    {
        Debug.Log("»щем доп комбинации");
        List<CellModel> cellsToDelete = new List<CellModel>();

        while (_cellsToSearchFrom.Count > 0)
        {
            cellsToDelete.Merge(FindShape(_cellsToSearchFrom.Dequeue(), TargetCoincidenceLength));
        }
        Debug.Log($"длина очереди после распаковки: {_cellsToSearchFrom.Count}");
        coordinatesToDelete = cellsToDelete.Select(cell => cell.Coordinates).Distinct();

        return cellsToDelete.Count > 0;
    }
}