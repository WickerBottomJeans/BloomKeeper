using DefaultNamespace;
using Petals;
using UnityEngine;

public class PetalViewManager : MonoBehaviour
{
    [SerializeField] private PetalView petalViewPrefab;
    [SerializeField] private PetalSpriteConfig petalSpriteConfig;

    private PetalView[,] petalViews;
    private UIGameBoard board;

    public void Init(Tile[,] grid, UIGameBoard board)
    {
        this.board = board;
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);
        petalViews = new PetalView[cols, rows];

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Tile tile = grid[x, y];
                if (tile is InactiveTile || tile.Petal == null) continue;

                Vector2 pos = board.GetCellWorldPos(x, y);
                PetalView view = Instantiate(petalViewPrefab, pos, Quaternion.identity, transform);
                view.Init(tile.Petal, board.CellSize, petalSpriteConfig);
                petalViews[x, y] = view;
            }
        }
    }

    public PetalView GetView(int x, int y) => petalViews[x, y];
}