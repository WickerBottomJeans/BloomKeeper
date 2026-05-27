using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using Petals;
using UnityEditor;
using UnityEngine;
using Utility;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class UIGameBoard : MonoBehaviour
{
    [SerializeField] private float paddingX = 0.05f;
    [SerializeField] private float paddingY = 0.05f;
    [SerializeField] private PetalViewManager petalViewManager;
    [SerializeField] private BoardInputHandler boardInputHandler;

    private Camera cam;
    private BoardLayout layout;
    private MeshFilter meshFilter;
    private SwapController swapController;
    private Tile[,] grid;

    private BoardState currentState = BoardState.Idle;
    private Vector2Int pendingCellA;
    private Vector2Int pendingCellB;
    private List<MatchGroup> pendingMatches;
    private List<(Vector2Int from, Vector2Int to)> pendingMoves;

    private enum BoardState { Idle, Swapping, SwappingBack, Resolving, Gravity, Filling, Cascade, Shuffling }
    
    public void Init(Tile[,] grid)
    {
        cam = Camera.main;
        this.grid = grid;
        meshFilter = GetComponent<MeshFilter>();

        layout = BoardLayoutCalculator.Calculate(
            grid.GetLength(0), grid.GetLength(1), cam, paddingX, paddingY);

        meshFilter.mesh = BoardMeshBuilder.BuildFillMesh(grid, layout);

        petalViewManager.Init(grid, layout);
        boardInputHandler.Init(layout, cam);

        swapController = new SwapController();
        boardInputHandler.OnSwapRequested += HandleSwap;
    }

    private void HandleSwap(Vector2Int cellA, Vector2Int cellB)
    {
        if (currentState != BoardState.Idle) return;
        if (!swapController.Validate(cellA, cellB, grid)) return;

        pendingCellA = cellA;
        pendingCellB = cellB;
        TransitionTo(BoardState.Swapping);
    }

    private async void TransitionTo(BoardState newState)
    {
        currentState = newState;
        await UniTask.NextFrame();
        switch (newState)
        {
            case BoardState.Swapping:     EnterSwapping();     break;
            case BoardState.SwappingBack: EnterSwappingBack(); break;
            case BoardState.Resolving:    EnterResolving();    break;
            case BoardState.Gravity:      EnterGravity();      break;
            case BoardState.Filling:      EnterFilling();      break;
            case BoardState.Cascade:      EnterCascade();      break;
            case BoardState.Idle:         EnterIdle();         break;
            case BoardState.Shuffling:    EnterShuffling(); break;

        }
    }

    private void EnterSwapping()
    {
        swapController.ExecuteSwapPetal(pendingCellA, pendingCellB, grid);
        pendingMatches = MatchDetector.Detect(grid);

        if (pendingMatches.Count == 0)
        {
            swapController.ExecuteSwapPetal(pendingCellA, pendingCellB, grid);
            petalViewManager.OnSwap(pendingCellA, pendingCellB, () => TransitionTo(BoardState.SwappingBack));
            return;
        }

        petalViewManager.OnSwap(pendingCellA, pendingCellB, () => TransitionTo(BoardState.Resolving));
    }

    private void EnterSwappingBack()
    {
        petalViewManager.OnSwap(pendingCellB, pendingCellA, () => TransitionTo(BoardState.Idle));
    }

    private void EnterResolving()
    {
        MatchResolver.Resolve(pendingMatches, grid);
        petalViewManager.OnMatchResolved(pendingMatches, () => TransitionTo(BoardState.Gravity));
    }

    private void EnterGravity()
    {
        pendingMoves = GravityController.Apply(grid);
        petalViewManager.OnGravityApplied(pendingMoves, layout, () => TransitionTo(BoardState.Filling));
    }

    private void EnterFilling()
    {
        List<Vector2Int> filled = PetalFiller.Fill(grid);
        petalViewManager.OnFilled(filled, layout, grid, () => TransitionTo(BoardState.Cascade));
    }

    private void EnterCascade()
    {
        List<MatchGroup> cascadeMatches = MatchDetector.Detect(grid);
        if (cascadeMatches.Count == 0)
        {
            TransitionTo(BoardState.Idle);
            return;
        }

        pendingMatches = cascadeMatches;
        TransitionTo(BoardState.Resolving);
    }

    private void EnterIdle()
    {
        if (!DeadlockDetector.HasValidMove(grid))
            TransitionTo(BoardState.Shuffling);
    }
    
    private void EnterShuffling()
    {
        List<Vector2Int> shuffled = BoardShuffler.Shuffle(grid);
        petalViewManager.OnShuffled(shuffled, layout, grid, () => TransitionTo(BoardState.Cascade));
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Tile tile = grid[x, y];
                Vector2 center = layout.GetCellWorldPos(x, y);

                Gizmos.color = tile switch
                {
                    InactiveTile => new Color(0.55f, 0.1f, 0.1f, 0.8f),
                    WebTile      => new Color(0.6f, 0.6f, 0.6f, 0.8f),
                    NormalTile   => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                    _            => Color.white
                };

                Gizmos.DrawWireCube(center, Vector3.one * layout.CellSize * 0.95f);
            }
        }
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Tile tile = grid[x, y];
                if (tile?.Petal == null) continue;
                Vector2 center = layout.GetCellWorldPos(x, y);
                Handles.Label(center, tile.Petal.PetalType.ToString()[0].ToString());
                int i = 1;
            }
        }
    }
 
}
