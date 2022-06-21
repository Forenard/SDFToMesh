using UnityEngine;
public static class MarchingCube
{
    private static Triangle[][] _table = null;
    public static Triangle[][] Table
    {
        get
        {
            if (_table == null)
            {
                // format:
                //   _table[ID] = [TRIANGLE, TRIANGLE, ...]
                // where
                //   TRIANGLE := [EDGE, EDGE, EDGE]
                //   EDGE := [EDGE_CENTER_X, EDGE_CENTER_Y, EDGE_CENTER_Z]
                // ID is an integer from 0 to 255, where the i'th bit of it represents whether the corresponding vertex is inside(0) or outside(1).
                //   i = 0: vertex at (-1, -1, -1)
                //       1: vertex at (-1, -1,  1)
                //       2: vertex at (-1,  1, -1)
                //       3: vertex at (-1,  1,  1)
                //       4: vertex at ( 1, -1, -1)
                //       5: vertex at ( 1, -1,  1)
                //       6: vertex at ( 1,  1, -1)
                //       7: vertex at ( 1,  1,  1)
                var root = JsonNode.Parse(Resources.Load<TextAsset>("MarchingCubeTable").text);
                int[][][][] rawtable = new int[256][][][];
                for (int i = 0; i < 256; i++)
                {
                    rawtable[i] = new int[root[i].Count][][];
                    for (int j = 0; j < root[i].Count; j++)
                    {
                        rawtable[i][j] = new int[root[i][j].Count][];
                        for (int k = 0; k < root[i][j].Count; k++)
                        {
                            rawtable[i][j][k] = new int[root[i][j][k].Count];
                            for (int l = 0; l < root[i][j][k].Count; l++)
                            {
                                rawtable[i][j][k][l] = int.Parse(root[i][j][k][l].Get<string>());
                            }
                        }
                    }
                }
                // rawtableをtableの形式に変換
                _table = new Triangle[256][];
                for (int i = 0; i < 256; i++)
                {
                    _table[i] = new Triangle[rawtable[i].Length];
                    for (int j = 0; j < rawtable[i].Length; j++)
                    {
                        _table[i][j] = new Triangle(
                            new Vector3Int(rawtable[i][j][0][0], rawtable[i][j][0][1], rawtable[i][j][0][2]),
                            new Vector3Int(rawtable[i][j][1][0], rawtable[i][j][1][1], rawtable[i][j][1][2]),
                            new Vector3Int(rawtable[i][j][2][0], rawtable[i][j][2][1], rawtable[i][j][2][2])
                        );
                    }
                }
            }
            return _table;
        }
    }
}