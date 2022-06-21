using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SDFToMesh : MonoBehaviour
{
    [SerializeField] float _size = 1f;
    [SerializeField] int _divide = 10;

    // https://iquilezles.org/articles/intersectors
    static Vector2 isphere(Vector4 sph, Vector3 ro, Vector3 rd)
    {
        Vector3 oc = ro - (Vector3)sph;
        float b = Vector3.Dot(oc, rd);
        float c = Vector3.Dot(oc, oc) - sph.w * sph.w;
        float h = b * b - c;
        if (h < 0.0) return new Vector2(-1.0f, -1.0f);
        h = Mathf.Sqrt(h);
        return new Vector2(-h - b, h - b);
    }

    // https://iquilezles.org/articles/mandelbulb
    static float map(Vector3 p)
    {
        Vector3 w = p;
        float m = Vector3.Dot(w, w);

        Vector4 trap = new Vector4(Mathf.Abs(w.x), Mathf.Abs(w.y), Mathf.Abs(w.z), m);
        float dz = 1.0f;

        for (int i = 0; i < 4; i++)
        {
            // polynomial version (no trigonometrics, but MUCH slower)
            float m2 = m * m;
            float m4 = m2 * m2;
            dz = 8.0f * Mathf.Sqrt(m4 * m2 * m) * dz + 1.0f;

            float x = w.x; float x2 = x * x; float x4 = x2 * x2;
            float y = w.y; float y2 = y * y; float y4 = y2 * y2;
            float z = w.z; float z2 = z * z; float z4 = z2 * z2;

            float k3 = x2 + z2;
            float k2 = 1.0f / Mathf.Sqrt(k3 * k3 * k3 * k3 * k3 * k3 * k3);
            float k1 = x4 + y4 + z4 - 6.0f * y2 * z2 - 6.0f * x2 * y2 + 2.0f * z2 * x2;
            float k4 = x2 - y2 + z2;

            w.x = p.x + 64.0f * x * y * z * (x2 - z2) * k4 * (x4 - 6.0f * x2 * z2 + z4) * k1 * k2;
            w.y = p.y + -16.0f * y2 * k3 * k4 * k4 + k1 * k1;
            w.z = p.z + -8.0f * y * k4 * (x4 * x4 - 28.0f * x4 * x2 * z2 + 70.0f * x4 * z4 - 28.0f * x2 * z2 * z4 + z4 * z4) * k1 * k2;

            trap = Vector4.Min(trap, new Vector4(Mathf.Abs(w.x), Mathf.Abs(w.y), Mathf.Abs(w.z), m));

            m = Vector3.Dot(w, w);
            if (m > 256.0f)
                break;
        }

        // distance estimation (through the Hubbard-Douady potential)
        return 0.25f * Mathf.Log(m) * Mathf.Sqrt(m) / dz;
    }
    SDF _sdf = (Vector3 pos, float t) =>
    {
        return map(pos);
    };
    [ContextMenu("InitMesh")]
    public void InitMesh()
    {
        MeshFilter _mf = GetComponent<MeshFilter>();
        _mf.mesh = new Mesh();
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        CreateMesh(_mf.sharedMesh, _sdf, _size, _divide);
        stopwatch.Stop();
        Debug.Log($"CreateMesh: {stopwatch.ElapsedMilliseconds}ms");
    }
    private void Awake() => InitMesh();
    public static void CreateMesh(Mesh mesh, SDF sdf, float size, int divide, float t = 0)
    {
        List<int> triangles = new List<int>();
        Dictionary<Vector3Int, int> vertexMap = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < divide; i++)
        {
            for (int j = 0; j < divide; j++)
            {
                for (int k = 0; k < divide; k++)
                {
                    Vector3Int[] cube = GetCubePositions();
                    Vector3Int offset = new Vector3Int(i, j, k);
                    byte tableid = 0b00000000;
                    for (int l = 0; l < 8; l++)
                    {
                        Vector3 pos = ToWorldPosition(cube[l], offset, size, divide);
                        bool inside = sdf(pos, t) < 0;
                        tableid |= (byte)(inside ? 0 : 1 << l);
                    }
                    Triangle[] polygons = MarchingCube.Table[tableid];
                    for (int l = 0; l < polygons.Length; l++)
                    {
                        Triangle polygon = polygons[l];
                        for (int m = 0; m < 3; m++)
                        {
                            Vector3Int vert = polygon.poses[m];
                            vert += offset * 2;
                            if (vertexMap.ContainsKey(vert))
                            {
                                int tri = vertexMap[vert];
                                triangles.Add(tri);
                            }
                            else
                            {
                                int tri = vertexMap.Count;
                                vertexMap.Add(vert, tri);
                                triangles.Add(tri);
                            }
                        }
                    }
                }
            }
        }

        List<Vector3> vertices = new List<Vector3>(new Vector3[vertexMap.Count]);
        foreach (var pair in vertexMap)
        {
            int ind = pair.Value;
            Vector3Int pos = pair.Key;
            Vector3 worldPos = ToWorldPosition(pos, size, divide);
            vertices[ind] = worldPos;
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    private static Vector3 ToWorldPosition(Vector3Int cubepos, Vector3Int offsetpos, float size, int divide)
    {
        float boxsize = size / divide;
        return new Vector3(
            boxsize * (0.5f + offsetpos.x) - size * 0.5f + cubepos.x * boxsize * 0.5f,
            boxsize * (0.5f + offsetpos.y) - size * 0.5f + cubepos.y * boxsize * 0.5f,
            boxsize * (0.5f + offsetpos.z) - size * 0.5f + cubepos.z * boxsize * 0.5f
        );
    }
    private static Vector3 ToWorldPosition(Vector3Int localpos, float size, int divide)
    {
        float boxsize = size / divide;
        return new Vector3(
            boxsize * (0.5f + localpos.x * 0.5f) - size * 0.5f,
            boxsize * (0.5f + localpos.y * 0.5f) - size * 0.5f,
            boxsize * (0.5f + localpos.z * 0.5f) - size * 0.5f
        );
    }

    private static Vector3Int[] GetCubePositions()
    {
        return new Vector3Int[]
        {
            new Vector3Int(-1, -1, -1),
            new Vector3Int(-1, -1,  1),
            new Vector3Int(-1,  1, -1),
            new Vector3Int(-1,  1,  1),
            new Vector3Int( 1, -1, -1),
            new Vector3Int( 1, -1,  1),
            new Vector3Int( 1,  1, -1),
            new Vector3Int( 1,  1,  1),
        };
    }
    public delegate float SDF(Vector3 pos, float t);
}
