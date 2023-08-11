using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellModel
{
    public int Value { get; private set; }

    public readonly Vector2Int Coordinates;
    public readonly int Id;

    public CellModel(int value, int x, int y, int id)
    {
        Value = value;
        Coordinates = new Vector2Int(x, y);
        Id = id;
    }

    public void SetValue(int value)
    {
        Value = value;
    }
}
