using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

public static class BoardMeshBuilder
{
    public static Mesh BuildFillMesh(Tile[,] grid, BoardLayout layout, float textureAspect)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);
        float half = layout.CellSize / 2f;

        float totalWidth = cols * layout.CellSize;
        float totalHeight = rows * layout.CellSize;
        float boardAspect = totalWidth / totalHeight;

        float scaleU, scaleV, offsetU, offsetV;
        if (boardAspect > textureAspect)
        {
            scaleU = 1f;
            scaleV = textureAspect / boardAspect;
            offsetU = 0f;
            offsetV = (1f - scaleV) / 2f;
        }
        else
        {
            scaleU = boardAspect / textureAspect;
            scaleV = 1f;
            offsetU = (1f - scaleU) / 2f;
            offsetV = 0f;
        }

        float boardLeft = layout.OriginWorldPos.x - half;
        float boardBottom = layout.OriginWorldPos.y - half;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] is InactiveTile) continue;

                Vector2 center = layout.OriginWorldPos + new Vector2(x * layout.CellSize, y * layout.CellSize);

                int vIndex = vertices.Count;

                Vector3 bl = new Vector3(center.x - half, center.y - half, 0);
                Vector3 tl = new Vector3(center.x - half, center.y + half, 0);
                Vector3 tr = new Vector3(center.x + half, center.y + half, 0);
                Vector3 br = new Vector3(center.x + half, center.y - half, 0);

                vertices.Add(bl);
                vertices.Add(tl);
                vertices.Add(tr);
                vertices.Add(br);

                uvs.Add(new Vector2(offsetU + ((bl.x - boardLeft) / totalWidth) * scaleU,
                    offsetV + ((bl.y - boardBottom) / totalHeight) * scaleV));
                uvs.Add(new Vector2(offsetU + ((tl.x - boardLeft) / totalWidth) * scaleU,
                    offsetV + ((tl.y - boardBottom) / totalHeight) * scaleV));
                uvs.Add(new Vector2(offsetU + ((tr.x - boardLeft) / totalWidth) * scaleU,
                    offsetV + ((tr.y - boardBottom) / totalHeight) * scaleV));
                uvs.Add(new Vector2(offsetU + ((br.x - boardLeft) / totalWidth) * scaleU,
                    offsetV + ((br.y - boardBottom) / totalHeight) * scaleV));

                triangles.Add(vIndex);
                triangles.Add(vIndex + 1);
                triangles.Add(vIndex + 2);
                triangles.Add(vIndex);
                triangles.Add(vIndex + 2);
                triangles.Add(vIndex + 3);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}