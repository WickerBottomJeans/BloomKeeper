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

    public async UniTask OnPetalsChanged(
        IReadOnlyList<PetalChange> changes,
        BoardLayout boardLayout,
        float duration)
    {
        var tasks = new List<UniTask>();

        foreach (PetalChange change in changes)
        {
            PetalView view = petalViews[change.Position.x, change.Position.y];
            if (view == null) continue;

            tasks.Add(PetalViewAnimator.PlayPetalChange(
                view,
                change.After,
                boardLayout.CellSize,
                duration));
        }

        await UniTask.WhenAll(tasks);
    }

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

    public async UniTask PlayDisappearAndRelease(
        IReadOnlyList<Vector2Int> positions,
        float duration)
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

    public async UniTask PlayStripedDisappear(Vector2Int source, bool isVertical, float duration)
    {
        var disappearTasks = new List<UniTask>();
        int maxDistance = 0;
        int lineLength = isVertical ? petalViews.GetLength(1) : petalViews.GetLength(0);

        //Get the max distance from the source to the sides
        for (int i = 0; i < lineLength; i++)
        {
            int x = isVertical ? source.x : i;
            int y = isVertical ? i : source.y;
            if (petalViews[x, y] != null)
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(i - (isVertical ? source.y : source.x)));
        }
        
        float stepDuration = maxDistance > 0 ? duration / maxDistance : duration;

        for (int distance = 0; distance <= maxDistance; distance++)
        {
            int directionCount = distance == 0 ? 1 : 2;

            for (int direction = 0; direction < directionCount; direction++)
            {
                int offset = direction == 0 ? distance : -distance;
                Vector2Int position = isVertical ? new Vector2Int(source.x, source.y + offset) : new Vector2Int(source.x + offset, source.y);

                if (position.x < 0 || position.x >= petalViews.GetLength(0) || position.y < 0 || position.y >= petalViews.GetLength(1))
                    continue;

                PetalView view = petalViews[position.x, position.y];
                if (view == null) continue;

                petalViews[position.x, position.y] = null;
                disappearTasks.Add(DisappearAndRelease(view, stepDuration));
            }

            if (distance < maxDistance)
                await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
        }

        await UniTask.WhenAll(disappearTasks);
    }

    private async UniTask DisappearAndRelease(PetalView view, float duration)
    {
        await PetalViewAnimator.PlayDisappear(view, duration);
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

    public UniTask PlayComboMerge(Vector2Int sourceA, Vector2Int sourceB)
    {
        PetalView viewA = petalViews[sourceA.x, sourceA.y];
        PetalView viewB = petalViews[sourceB.x, sourceB.y];
        return PetalViewAnimator.PlayComboMerge(viewA, viewB);
    }

    public async UniTask PlayComboSpinAndRelease(
        Vector2Int sourceA,
        Vector2Int sourceB,
        float duration)
    {
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
