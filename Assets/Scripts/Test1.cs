using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.Events;
using System.Collections;

public class Test1 : MonoBehaviour
{
    private void Start()
    {
        GridModel1 testModel = new GridModel1(new Vector2Int(4, 4), 2, 3);
        testModel.SearchAdditionalPaths(out IEnumerable<Vector2Int> coordinatesToDelete);
    }
}

public class GridModel1
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

    public GridModel1(Vector2Int size, int cellTypeCount, int targetCoincidenceLength)
    {
        _grid = new CellModel[4, 4];
        _cellTypeCount = cellTypeCount;
        int id = 0;

        _grid = new CellModel[4, 4];

        _grid[0, 0] = new CellModel(0, 0, 0, id++);
        _grid[1, 0] = new CellModel(1, 1, 0, id++);
        _grid[2, 0] = new CellModel(0, 2, 0, id++);
        _grid[3, 0] = new CellModel(0, 3, 0, id++);

        _grid[0, 1] = new CellModel(1, 0, 1, id++);
        _grid[1, 1] = new CellModel(0, 1, 1, id++);
        _grid[2, 1] = new CellModel(1, 2, 1, id++);
        _grid[3, 1] = new CellModel(0, 3, 1, id++);

        _grid[0, 2] = new CellModel(0, 0, 2, id++);
        _grid[1, 2] = new CellModel(1, 1, 2, id++);
        _grid[2, 2] = new CellModel(0, 2, 2, id++);
        _grid[3, 2] = new CellModel(0, 3, 2, id++);

        _grid[0, 3] = new CellModel(1, 0, 3, id++);
        _grid[1, 3] = new CellModel(1, 1, 3, id++);
        _grid[2, 3] = new CellModel(1, 2, 3, id++);
        _grid[3, 3] = new CellModel(1, 3, 3, id++);

        TargetCoincidenceLength = targetCoincidenceLength;

        _cellsToSearchFrom.Enqueue(_grid[0,0]);
        _cellsToSearchFrom.Enqueue(_grid[0,1]);
        _cellsToSearchFrom.Enqueue(_grid[0,2]);
        _cellsToSearchFrom.Enqueue(_grid[0,3]);
        _cellsToSearchFrom.Enqueue(_grid[1,3]);
        _cellsToSearchFrom.Enqueue(_grid[2,3]);
        _cellsToSearchFrom.Enqueue(_grid[3,3]);
        _cellsToSearchFrom.Enqueue(_grid[3,2]);
        _cellsToSearchFrom.Enqueue(_grid[3,1]);
        _cellsToSearchFrom.Enqueue(_grid[3,0]);
    }


    public IEnumerable<CellModel> FindShape(CellModel start, int targetMatchValue)
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

        yield return current; // ��� ��� �����? ������ ������ ������ ������������, ���� �� 2 ����� � ����������� ����������
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

        int counter = 0; //��� ������

        while (TryMove(start, direction, out CellModel result) && start.Value == result.Value)
        {
            counter++; // ��� ������

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
                deletedCellsAmount = amountToDelete;
            }

            for (int i = bottom; i < _grid.GetLength(1) - 1; i++)
            {
                _cellsToSearchFrom.Enqueue(_grid[group.Key, i]);
            }
        }
    }

    private IEnumerable<ColumnPart1> DivideIntoGroups(IGrouping<int, CellModel> group)
    {
        var cells = group.OrderByDescending(cell => cell.Coordinates.y).ToArray();

        Debug.Log($"������ �� ��������: {cells.Length} / LastIndex: {cells.Length - 2}");

        foreach (var cell in cells)
        {
            Debug.Log($"����������: {cell.Coordinates}");
        }

        int lastIndex = cells.Length - 2;
        int last = _grid.GetLength(1) - 1;

        if (cells.Length == 1)
        {
            yield return new ColumnPart1(this, group.Key, cells[0].Coordinates.y, last);
        }

        for (int i = 0; i <= lastIndex; i++)
        {
            Debug.Log("�� � �����");

            if (cells[i].Coordinates.y != cells[i + 1].Coordinates.y + 1)
            {

                Debug.Log($"cells[i]: {cells[i].Coordinates} / cells[i + 1]: {cells[i + 1].Coordinates} / i: {i}");

                CellModel start = cells[i];
                Debug.Log($"X ������: {group.Key} / ������: {start.Coordinates} / ������� Y: {last}");
                yield return new ColumnPart1(this, group.Key, start.Coordinates.y, last);

                //last = cells[i].Coordinates.y - 1;
            }
            if (i == lastIndex)
            {
                CellModel start = cells[i + 1];
                yield return new ColumnPart1(this, group.Key, start.Coordinates.y, last);
            }
        }
    }

    private void MoveCellsInColumn(int amountToDelete, ColumnPart1 column)
    {
        int fallsCount = column.Count - amountToDelete;

        Debug.Log($"fallsCount: {fallsCount}");

        for (int i = 0; i < fallsCount; i++)
        {
            Debug.Log($"� ������ {column[i].Coordinates} ������ {column[i + amountToDelete].Coordinates}");
            column[i].SetValue(column[i + amountToDelete].Value);
            CellFallenDown?.Invoke(column[i + amountToDelete].Coordinates, column[i].Coordinates);
        }

        for (int i = 0; i < amountToDelete; i++)
        {
            int random = RandomCellValue;
            Debug.Log($"� ������ {column[i + fallsCount].Coordinates} ��������� �������� {random}");
            column[i + fallsCount].SetValue(random);
            CellCreated?.Invoke(column[i + fallsCount].Coordinates, amountToDelete);
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
        List<CellModel> cellsToDelete = new List<CellModel>();

        while (_cellsToSearchFrom.Count > 0)
        {
            cellsToDelete.Merge(FindShape(_cellsToSearchFrom.Dequeue(), TargetCoincidenceLength));
        }

        coordinatesToDelete = cellsToDelete.Select(cell => cell.Coordinates).Distinct().ToList();

        return cellsToDelete.Count > 0;
    }
}

public struct ColumnPart1 : IEnumerable<CellModel>
{
    private readonly GridModel1 _gridModel;
    private readonly int _x;

    public readonly int Start;
    public readonly int End;

    public int Count => End + 1 - Start;

    public CellModel this[int y] => _gridModel[_x, y + Start];

    public ColumnPart1(GridModel1 gridModel, int x, int first, int last)
    {
        _gridModel = gridModel;
        _x = x;
        Start = first;
        End = last;
    }

    public IEnumerator<CellModel> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
