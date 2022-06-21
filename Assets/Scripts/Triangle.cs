using UnityEngine;

public struct Triangle
{
    public Vector3Int[] poses;
    public Triangle(Vector3Int a, Vector3Int b, Vector3Int c)
    {
        poses = new Vector3Int[] { a, b, c };
    }
}