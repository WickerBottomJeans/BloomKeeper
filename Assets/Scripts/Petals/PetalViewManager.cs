using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using Petals;
using UnityEngine;
using UnityEngine.Pool;

public class PetalViewManager : MonoBehaviour
{
    [SerializeField] private PetalView petalViewPrefab;

    private PetalView[,] petalViews;
    private ObjectPool<PetalView> pool;
    public void Init(Tile[,] grid, BoardLayout boardLayout)
    {
        pool = new ObjectPool<PetalView>(
            createFunc:      () => Instantiate(petalViewPrefab, transform),
            actionOnGet:     view => view.gameObject.SetActive(true),
            actionOnRelease: view => view.gameObject.SetActive(false),
            actionOnDestroy: view => Destroy(view.gameObject)
        );
        
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);
        petalViews = new PetalView[cols, rows];
        
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Tile tile = grid[x, y];
                if (tile is InactiveTile || tile.Petal == null) continue;

                Vector2 pos = boardLayout.GetCellWorldPos(x, y);
                PetalView view = pool.Get();
                view.transform.position = pos;
                view.Init(tile.Petal, boardLayout.CellSize);
                petalViews[x, y] = view;
            }
        }
    }

    public PetalView GetView(int x, int y) => petalViews[x, y];

    public async UniTask OnSwap(Vector2Int cellA, Vector2Int cellB)
    {
        PetalView viewA = petalViews[cellA.x, cellA.y];
        PetalView viewB = petalViews[cellB.x, cellB.y];

        await UniTask.WhenAll(
            PetalViewAnimator.PlaySwap(viewA, viewB.transform.position),
            PetalViewAnimator.PlaySwap(viewB, viewA.transform.position)
        );

        petalViews[cellA.x, cellA.y] = viewB;
        petalViews[cellB.x, cellB.y] = viewA;
    }
    public async UniTask OnMatchResolved(MatchResolveResult result, BoardLayout boardLayout)
    {
        await ClearPetalViews(result.ClearedPositions);
        await SpawnSkillPetalViews(result.SpawnedPetals, boardLayout);
    }

    private async UniTask ClearPetalViews(List<Vector2Int> cleared)
    {
        var tasks = new List<UniTask>();
        foreach (Vector2Int cell in cleared)
        {
            PetalView view = petalViews[cell.x, cell.y];
            if (view == null) continue;
            petalViews[cell.x, cell.y] = null;
            tasks.Add(DestroyAndRelease(view));
        }
        await UniTask.WhenAll(tasks);
    }

    private async UniTask DestroyAndRelease(PetalView view)
    {
        await PetalViewAnimator.PlayDestroy(view);
        pool.Release(view);
    }
    
    //TODO: make this async
    private async UniTask SpawnSkillPetalViews(
        List<(Vector2Int Position, PetalType PetalType, SpecialSkillType SkillType)> spawns,
        BoardLayout boardLayout)
    {
        var tasks = new List<UniTask>();

        foreach (var (pos, petalType, skillType) in spawns)
        {
            Vector2 worldPos = boardLayout.GetCellWorldPos(pos.x, pos.y);

            PetalView view = pool.Get();
            view.transform.position = worldPos;
            view.Init(new Petal(petalType, skillType), boardLayout.CellSize);

            petalViews[pos.x, pos.y] = view;

            tasks.Add(PetalViewAnimator.PlaySpawn(view));
        }

        await UniTask.WhenAll(tasks);
    }

    public async UniTask OnGravityApplied(List<(Vector2Int from, Vector2Int to)> moves, BoardLayout boardLayout)
    {
        var tasks = new List<UniTask>();
        foreach (var (from, to) in moves)
        {
            PetalView view = petalViews[from.x, from.y];
            if (view == null) continue;
            Vector2 targetPos = boardLayout.GetCellWorldPos(to.x, to.y);
            petalViews[to.x, to.y] = view;
            petalViews[from.x, from.y] = null;
            tasks.Add(PetalViewAnimator.PlayDrop(view, targetPos));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnFilled(List<Vector2Int> filledCells, BoardLayout boardLayout, Tile[,] grid)
    {
        filledCells.Sort((a, b) => a.y.CompareTo(b.y));
        var tasks = new List<UniTask>();
        foreach (Vector2Int cell in filledCells)
        {
            Vector2 targetPos = boardLayout.GetCellWorldPos(cell.x, cell.y);
            Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.Rows * boardLayout.CellSize);
            PetalView view = pool.Get();
            view.transform.position = spawnPos;
            view.Init(grid[cell.x, cell.y].Petal, boardLayout.CellSize);
            petalViews[cell.x, cell.y] = view;
            PetalViewAnimator.PlaySpawn(view);
            tasks.Add(PetalViewAnimator.PlayDrop(view, targetPos));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnShuffled(List<Vector2Int> cells, BoardLayout boardLayout, Tile[,] grid)
    {
        var tasks = new List<UniTask>();
        foreach (Vector2Int cell in cells)
        {
            PetalView view = petalViews[cell.x, cell.y];
            if (view == null) continue;
            tasks.Add(ShuffleCell(view, cell, cell.x * 0.05f, boardLayout, grid));
        }
        await UniTask.WhenAll(tasks);
    }
    
    private async UniTask ShuffleCell(PetalView view, Vector2Int cell, float delay, BoardLayout boardLayout, Tile[,] grid)
    {
        await PetalViewAnimator.PlayDestroy(view, delay);
        pool.Release(view);

        Vector2 targetPos = boardLayout.GetCellWorldPos(cell.x, cell.y);
        Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.Rows * boardLayout.CellSize);
        PetalView newView = pool.Get();
        newView.transform.position = spawnPos;
        newView.Init(grid[cell.x, cell.y].Petal, boardLayout.CellSize);
        petalViews[cell.x, cell.y] = newView;
        await PetalViewAnimator.PlaySpawn(newView);
        await PetalViewAnimator.PlayDrop(newView, targetPos);
    }
    
    public void RefreshCell(Vector2Int cell, Petal petal, BoardLayout boardLayout)
    {
        PetalView existing = petalViews[cell.x, cell.y];
        if (existing != null)
        {
            pool.Release(existing);
            petalViews[cell.x, cell.y] = null;
        }

        if (petal == null) return;

        Vector2 pos = boardLayout.GetCellWorldPos(cell.x, cell.y);
        PetalView view = pool.Get();
        view.transform.position = pos;
        view.Init(petal, boardLayout.CellSize);
        petalViews[cell.x, cell.y] = view;
    }
}