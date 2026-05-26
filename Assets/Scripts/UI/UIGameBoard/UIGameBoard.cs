using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;
using Utility;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class UIGameBoard : MonoBehaviour
{
    [SerializeField] private float paddingX = 0.05f;
    [SerializeField] private float paddingY = 0.05f;
    [SerializeField] private PetalViewManager petalViewManager;

    private Camera cam;
    private BoardLayout layout;
    private MeshFilter meshFilter;

    public void Init(Tile[,] grid)
    {
        cam        = Camera.main;
        meshFilter = GetComponent<MeshFilter>();

        layout = BoardLayoutCalculator.Calculate(
            grid.GetLength(0), grid.GetLength(1), cam, paddingX, paddingY);

        meshFilter.mesh = BoardMeshBuilder.BuildFillMesh(grid, layout);

        petalViewManager.Init(grid, this);
    }

    public Vector2 GetCellWorldPos(int x, int y) =>
        layout.OriginWorldPos + new Vector2(x * layout.CellSize, y * layout.CellSize);

    public float CellSize => layout.CellSize;
}