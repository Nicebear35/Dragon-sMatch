using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extentions
{
    public static void Merge<T>(this List<T> first, params IEnumerable<T>[] collections)
    {
        foreach (var collection in collections)
        {
            foreach (var cell in collection)
            {
                first.Add(cell);
            }
        }
    }

    public static IEnumerable<Path> SelectPathsToDelete(this IEnumerable<Path> paths, int targetMatchValue)
    {
        Path[] pathsToDebug = paths.ToArray();
        var pathGroups = paths.GroupBy(path => path.FirstCell).ToArray();

        Dictionary<Vector2Int, Path> directedPaths = new Dictionary<Vector2Int, Path>();

        foreach (var group in pathGroups)
        {
            if (group.Count() == 1)
            {
                Path first = group.First();

                if (first.Length >= targetMatchValue)
                {
                    yield return first;
                }
                continue;
            }

            foreach (var path in group)
            {
                directedPaths.TryAdd(path.Direction, path);
            }

            foreach (var item in SelectPaths(directedPaths, targetMatchValue))
            {
                yield return item;
            }

            directedPaths.Clear();
        }
    }

    private static IEnumerable<Path> SelectPaths(Dictionary<Vector2Int, Path> directedPaths, int targetMatchValue)
    {
        bool leftFound = directedPaths.TryGetValue(Vector2Int.left, out Path left);
        bool rightFound = directedPaths.TryGetValue(Vector2Int.right, out Path right);
        bool upFound = directedPaths.TryGetValue(Vector2Int.up, out Path up);
        bool downFound = directedPaths.TryGetValue(Vector2Int.down, out Path down);

        foreach (var item in SelectPairs(leftFound, rightFound, left, right))
        {
            yield return item;
        }

        foreach (var item in SelectPairs(upFound, downFound, up, down))
        {
            yield return item;
        }

        IEnumerable<Path> SelectPairs(bool firstFound, bool secondFound, Path first, Path second)
        {
            if (firstFound && secondFound && first.Length + second.Length - 1 >= targetMatchValue)
            {
                yield return first;
                yield return second;
            }
            else if (firstFound && !secondFound && first.Length >= targetMatchValue)
            {
                yield return first;
            }
            else if (!firstFound && secondFound && second.Length >= targetMatchValue)
            {
                yield return second;
            }
        }
    }

    private static IEnumerable<int> FibonacciNums()
    {
        int prePreviousValue = 1;
        int previousValue = 1;

        yield return 1;
        yield return 1;

        while (true)
        {
            int sum = prePreviousValue + previousValue;
            yield return sum;

            prePreviousValue = previousValue;
            previousValue = sum;
        }
    }
}
