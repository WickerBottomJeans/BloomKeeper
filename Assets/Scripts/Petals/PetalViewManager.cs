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
    //TODO: should this cache board layout, the layout dont really channge over time
    [SerializeField] private PetalView petalViewPrefab;

    private PetalView[,] petalViews;
    private ObjectPool<PetalView> pool;
    public void Init(BoardCell[,] grid, BoardLayout boardLayout)
    {
        pool = new ObjectPool<PetalView>(
            createFunc: () => Instantiate(petalViewPrefab, transform),
            actionOnGet: view => view.gameObject.SetActive(true),
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
                BoardCell cell = grid[x, y];
                if (cell.IsVoid || cell.Petal == null) continue;

                Vector2 pos = boardLayout.GetCellWorldPos(x, y);
                PetalView view = pool.Get();
                view.transform.position = pos;
                view.Init(cell.Petal, boardLayout.CellSize);
                petalViews[x, y] = view;
            }
        }
    }

    public PetalView GetView(int x, int y) => petalViews[x, y];

    public async UniTask OnPetalsChanged(IReadOnlyList<PetalChange> changes, BoardLayout boardLayout, float duration)
    {
        var tasks = new List<UniTask>();

        foreach (PetalChange change in changes)
        {
            PetalView view = petalViews[change.Position.x, change.Position.y];
            if (view == null) continue;

            tasks.Add(PetalViewAnimator.PlayPetalChange(view, change.After, boardLayout.CellSize, duration));
        }

        await UniTask.WhenAll(tasks);
    }

    public async UniTask OnSwap(Vector2Int cellA, Vector2Int cellB, float cellSize)
    {
        PetalView viewA = petalViews[cellA.x, cellA.y];
        PetalView viewB = petalViews[cellB.x, cellB.y];

        await UniTask.WhenAll(
            PetalViewAnimator.PlaySwap(viewA, viewB.transform.position, cellSize),
            PetalViewAnimator.PlaySwap(viewB, viewA.transform.position, cellSize)
        );

        petalViews[cellA.x, cellA.y] = viewB;
        petalViews[cellB.x, cellB.y] = viewA;
    }

    public UniTask PlayNormalRemovals(IReadOnlyList<Vector2Int> positions, float duration = 0f)
    {
        return ClearPetalViews(positions, duration);
    }

    private async UniTask ClearPetalViews(IReadOnlyList<Vector2Int> cleared, float duration)
    {
        var tasks = new List<UniTask>();
        foreach (Vector2Int cell in cleared)
        {
            PetalView view = petalViews[cell.x, cell.y];
            if (view == null) continue;
            petalViews[cell.x, cell.y] = null;
            tasks.Add(DestroyAndRelease(view, duration));
        }
        await UniTask.WhenAll(tasks);
    }

    private async UniTask DestroyAndRelease(PetalView view, float duration)
    {
        if (duration <= 0f)
            await PetalViewAnimator.PlayDestroy(view);
        else
            await PetalViewAnimator.PlayDisappear(view, duration);
        pool.Release(view);
    }

    public async UniTask PlayDisappearAndRelease(IReadOnlyList<Vector2Int> positions, float duration)
    {
        var tasks = new List<UniTask>();

        foreach (Vector2Int position in positions)
        {
            PetalView view = petalViews[position.x, position.y];
            if (view == null) continue;

            petalViews[position.x, position.y] = null;
            tasks.Add(DisappearAndRelease(view, duration));
        }

        await UniTask.WhenAll(tasks);
    }

    public UniTask PlayAboutToExecuteShake(IReadOnlyList<Vector2Int> positions)
    {
        foreach (Vector2Int position in positions)
        {
            PetalView view = petalViews[position.x, position.y];
            if (view == null) continue;

            PetalViewAnimator.PlayAboutToExecute(view);
        }

        return UniTask.CompletedTask;
    }
    
    private async UniTask DisappearAndRelease(PetalView view, float duration)
    {
        await PetalViewAnimator.PlayDisappear(view, duration);
        view.transform.localRotation = Quaternion.identity;
        pool.Release(view);
    }

    // TODO: Fix Butterfly appearing under things. Use VFX layer or sth
    public UniTask PlayFly(Vector2Int sourceCell, Vector2Int targetCell, BoardLayout boardLayout, float duration)
    {
        PetalView view = petalViews[sourceCell.x, sourceCell.y];
        if (view == null) return UniTask.CompletedTask;

        Vector2 targetWorldPosition = boardLayout.GetCellWorldPos(targetCell.x, targetCell.y);
        return PetalViewAnimator.PlayFly(view, targetWorldPosition, duration);
    }
    
    public async UniTask PlaySkillPetalCreations(IReadOnlyList<SkillPetalSpawn> spawns, BoardLayout boardLayout)
    {
        var tasks = new List<UniTask>();

        foreach (SkillPetalSpawn spawn in spawns)
            tasks.Add(PlaySkillPetalCreation(spawn, boardLayout));

        await UniTask.WhenAll(tasks);
    }

    private async UniTask PlaySkillPetalCreation(SkillPetalSpawn spawn, BoardLayout boardLayout)
    {
        Vector2 spawnWorldPosition = boardLayout.GetCellWorldPos(spawn.SpawnPosition.x, spawn.SpawnPosition.y);
        var contributorViews = new List<PetalView>(spawn.ContributorPositions.Count);
        var tasks = new List<UniTask>(spawn.ContributorPositions.Count);

        foreach (Vector2Int position in spawn.ContributorPositions)
        {
            PetalView contributorView = petalViews[position.x, position.y];
            petalViews[position.x, position.y] = null;
            contributorViews.Add(contributorView);
            tasks.Add(PetalViewAnimator.PlayGather(contributorView, spawnWorldPosition));
        }

        await UniTask.WhenAll(tasks);

        foreach (PetalView contributorView in contributorViews)
            pool.Release(contributorView);

        PetalView skillPetalView = pool.Get();
        skillPetalView.transform.position = spawnWorldPosition;
        skillPetalView.Init(new Petal(spawn.PetalType, spawn.SkillType), boardLayout.CellSize);
        Color color = skillPetalView.spriteRenderer.color;
        color.a = 1f;
        skillPetalView.spriteRenderer.color = color;
        petalViews[spawn.SpawnPosition.x, spawn.SpawnPosition.y] = skillPetalView;
    }

    public UniTask PlayComboMerge(Vector2Int sourceA, Vector2Int sourceB)
    {
        PetalView viewA = petalViews[sourceA.x, sourceA.y];
        PetalView viewB = petalViews[sourceB.x, sourceB.y];
        return PetalViewAnimator.PlayComboMerge(viewA, viewB);
    }

    public async UniTask PlayComboSpinAndRelease(Vector2Int sourceA, Vector2Int sourceB, float duration)
    {
        //TODO: bug, when sun burst dont get executed from a swap, it wouldnt have 2 view. count for that case too
        PetalView viewA = petalViews[sourceA.x, sourceA.y];
        PetalView viewB = petalViews[sourceB.x, sourceB.y];

        await PetalViewAnimator.PlayComboSpinAndDisappear(viewA, viewB, duration);

        petalViews[sourceA.x, sourceA.y] = null;
        petalViews[sourceB.x, sourceB.y] = null;
        pool.Release(viewA);
        pool.Release(viewB);
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
            tasks.Add(PetalViewAnimator.PlayDrop(view, targetPos, boardLayout.CellSize));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnFilled(List<Vector2Int> filledCells, BoardLayout boardLayout, BoardCell[,] grid)
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
            PetalViewAnimator.PlaySpawn(view).Forget();
            tasks.Add(PetalViewAnimator.PlayDrop(view, targetPos, boardLayout.CellSize));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnShuffled(List<Vector2Int> cells, BoardLayout boardLayout, BoardCell[,] grid)
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
    
    private async UniTask ShuffleCell(PetalView view, Vector2Int cell, float delay, BoardLayout boardLayout, BoardCell[,] grid)
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
        await PetalViewAnimator.PlayDrop(newView, targetPos, boardLayout.CellSize);
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
