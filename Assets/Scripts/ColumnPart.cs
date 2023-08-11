using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public struct ColumnPart : IEnumerable<CellModel>
{
    private readonly GridModel _gridModel;
    private readonly int _x;

    public readonly int Start;
    public readonly int End;

    public int Count => End + 1 - Start;

    public CellModel this[int y] => _gridModel[_x, y + Start];

    public ColumnPart(GridModel gridModel, int x, int first, int last)
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
