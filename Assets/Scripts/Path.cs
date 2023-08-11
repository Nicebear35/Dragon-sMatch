using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Path : IEnumerable<CellModel>
{
    private readonly Queue<CellModel> _cells;
    public readonly Vector2Int Direction;

    public CellModel FirstCell => _cells.First();
    public int Length => _cells.Count;

    public Path(CellModel start, Vector2Int direction)
    {
        _cells = new Queue<CellModel>();
        _cells.Enqueue(start);
        Direction = direction;
    }

    public void Add(CellModel current)
    {
        _cells.Enqueue(current);
    }

    public IEnumerator<CellModel> GetEnumerator() => _cells.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _cells.GetEnumerator();
}
