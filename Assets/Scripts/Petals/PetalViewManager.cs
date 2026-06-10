using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using Petals;
using UnityEngine;
using UnityEngine.Pool;

public class PetalViewManager : MonoBehaviour
{
    [SerializeField] private PetalView petalViewPrefab;
    [SerializeField] private PetalSpriteConfig petalSpriteConfig;

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
                view.Init(tile.Petal, boardLayout.CellSize, petalSpriteConfig);
                petalViews[x, y] = view;
            }
        }
    }

    public PetalView GetView(int x, int y) => petalViews[x, y];

    public void OnSwap(Vector2Int cellA, Vector2Int cellB, Action onComplete = null)
    {
        PetalView viewA = petalViews[cellA.x, cellA.y];
        PetalView viewB = petalViews[cellB.x, cellB.y];

        Vector2 posA = viewA.transform.position;
        Vector2 posB = viewB.transform.position;

        bool aDone = false;
        bool bDone = false;

        PetalViewAnimator.PlaySwap(viewA, posB, () =>
        {
            aDone = true;
            if (bDone)
            {
                petalViews[cellA.x, cellA.y] = viewB;
                petalViews[cellB.x, cellB.y] = viewA;
                onComplete?.Invoke();
            }
        });

        PetalViewAnimator.PlaySwap(viewB, posA, () =>
        {
            bDone = true;
            if (aDone)
            {
                petalViews[cellA.x, cellA.y] = viewB;
                petalViews[cellB.x, cellB.y] = viewA;
                onComplete?.Invoke();
            }
        });
    }

    public void OnMatchResolved(MatchResolveResult result, BoardLayout boardLayout, Action onComplete = null)
    {
        ClearPetalViews(result.ClearedPositions, () =>
        {
            SpawnSkillPetalViews(result.SpawnedPetals, boardLayout);
            onComplete?.Invoke();
        });
    }

    private void ClearPetalViews(List<Vector2Int> cleared, Action onComplete)
    {
        int total = 0;
        int done = 0;

        foreach (Vector2Int cell in cleared)
            if (petalViews[cell.x, cell.y] != null) total++;

        if (total == 0) { onComplete?.Invoke(); return; }

        foreach (Vector2Int cell in cleared)
        {
            PetalView view = petalViews[cell.x, cell.y];
            if (view == null) continue;

            petalViews[cell.x, cell.y] = null;
            PetalViewAnimator.PlayDestroy(view, onComplete: () =>
            {
                pool.Release(view);
                done++;
                if (done == total) onComplete?.Invoke();
            });
        }
    }

    private void SpawnSkillPetalViews(List<(Vector2Int Position, PetalType PetalType, SpecialSkillType SkillType)> spawns, BoardLayout boardLayout)
    {
        foreach (var (pos, petalType, skillType) in spawns)
        {
            Vector2 worldPos = boardLayout.GetCellWorldPos(pos.x, pos.y);
            PetalView view = pool.Get();
            view.transform.position = worldPos;
            view.Init(new Petal(petalType, skillType), boardLayout.CellSize, petalSpriteConfig);
            petalViews[pos.x, pos.y] = view;
            PetalViewAnimator.PlaySpawn(view);
        }
    }

    public void OnGravityApplied(List<(Vector2Int from, Vector2Int to)> moves, BoardLayout boardLayout, Action onComplete = null)
    {
        int total = moves.Count;
        int done = 0;

        if (total == 0) { onComplete?.Invoke(); return; }

        foreach (var (from, to) in moves)
        {
            PetalView view = petalViews[from.x, from.y];
            if (view == null) continue;

            Vector2 targetPos = boardLayout.GetCellWorldPos(to.x, to.y);
            petalViews[to.x, to.y] = view;
            petalViews[from.x, from.y] = null;

            PetalViewAnimator.PlayDrop(view, targetPos, () =>
            {
                done++;
                if (done == total) onComplete?.Invoke();
            });
        }
    }
    
    public void OnFilled(List<Vector2Int> filledCells, BoardLayout boardLayout, Tile[,] grid, Action onComplete = null)
    {
        if (filledCells.Count == 0) { onComplete?.Invoke(); return; }

        filledCells.Sort((a, b) => a.y.CompareTo(b.y));

        int total = filledCells.Count;
        int done = 0;

        foreach (Vector2Int cell in filledCells)
        {
            Vector2 targetPos = boardLayout.GetCellWorldPos(cell.x, cell.y);
            Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.Rows * boardLayout.CellSize);

            PetalView view = pool.Get();
            view.transform.position = spawnPos;
            view.Init(grid[cell.x, cell.y].Petal, boardLayout.CellSize, petalSpriteConfig);
            petalViews[cell.x, cell.y] = view;

            PetalViewAnimator.PlaySpawn(view);
            PetalViewAnimator.PlayDrop(view, targetPos, () =>
            {
                done++;
                if (done == total) onComplete?.Invoke();
            });
        }
    }
    
    public void OnShuffled(List<Vector2Int> cells, BoardLayout boardLayout, Tile[,] grid, Action onComplete = null)
    {
        float delayPerColumn = 0.05f;
        int total = cells.Count;
        int done = 0;

        foreach (Vector2Int cell in cells)
        {
            PetalView view = petalViews[cell.x, cell.y];
            if (view == null) continue;

            float delay = cell.x * delayPerColumn;
            petalViews[cell.x, cell.y] = null;

            PetalViewAnimator.PlayDestroy(view, delay, () =>
            {
                pool.Release(view);

                Vector2 targetPos = boardLayout.GetCellWorldPos(cell.x, cell.y);
                Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.Rows * boardLayout.CellSize);

                PetalView newView = pool.Get();
                newView.transform.position = spawnPos;
                newView.Init(grid[cell.x, cell.y].Petal, boardLayout.CellSize, petalSpriteConfig);
                petalViews[cell.x, cell.y] = newView;

                PetalViewAnimator.PlaySpawn(newView);
                PetalViewAnimator.PlayDrop(newView, targetPos, () =>
                {
                    done++;
                    if (done == total) onComplete?.Invoke();
                });
            });
        }
    }
    
    public static string GetPetalSpriteKey(PetalType type, SpecialSkillType skill)
    {
        string skillName = skill == SpecialSkillType.None ? "Default" : skill.ToString();
        return $"{type}_{skillName}";
    }
    
}