using UnityEngine;
using UnityEngine.SocialPlatforms;

public struct CoordinateGrid 
{
    private readonly float _scale;
    private readonly Vector3 _startPosition;


    public Vector3 this[int x, int y] => new Vector3(x * _scale, y * _scale, 0) + _startPosition;

    public CoordinateGrid(float scale, Vector3 startPosition)
    {
        _scale = scale;
        _startPosition = startPosition;
    }
}
