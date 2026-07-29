using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DG.Tweening;
using Petals;
using UnityEngine;
using UnityEngine.Pool;

public class PetalViewManager : MonoBehaviour
{
    private const float DefaultDisappearDuration = 0.2f;

    //TODO: should this cache board layout, the layout dont really channge over time
    [SerializeField] private PetalView petalViewPrefab;

    private PetalView[,] petalViews;
    private ObjectPool<PetalView> pool;
    private readonly Dictionary<PetalView, ViewAccessKey> activeUses = new Dictionary<PetalView, ViewAccessKey>();

    public void Init(Tile[,] grid, BoardLayout boardLayout)
    {
        pool = new ObjectPool<PetalView>(
            createFunc: () => Instantiate(petalViewPrefab, transform),
            actionOnGet: view => view.gameObject.SetActive(true),
            actionOnRelease: view =>
            {
                view.ResetForPool();
                view.gameObject.SetActive(false);
            },
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
                if (tile == null || tile.Petal == null) continue;

                Vector2 pos = boardLayout.GetTileWorldPos(x, y);
                PetalView view = pool.Get();
                view.transform.position = pos;
                view.Init(tile.Petal, boardLayout.TileSize);
                petalViews[x, y] = view;
            }
        }
    }

    public bool TryAcquireView(Vector2Int position, string userName, out ViewAccessKey accessKey)
    {
        PetalView view = petalViews[position.x, position.y];
        if (view == null || activeUses.ContainsKey(view))
        {
            accessKey = null;
            return false;
        }

        accessKey = new ViewAccessKey(view, position, userName);
        activeUses.Add(view, accessKey);
        return true;
    }

    public void ReleaseView(ViewAccessKey accessKey)
    {
        if (!activeUses.TryGetValue(accessKey.View, out ViewAccessKey currentAccessKey))
            throw new InvalidOperationException($"Petal view access held by '{accessKey.UserName}' is not active.");
        if (currentAccessKey != accessKey)
            throw new InvalidOperationException($"Petal view access held by '{accessKey.UserName}' does not match the active access key.");
        activeUses.Remove(accessKey.View);
    }

    public async UniTask OnPetalsChanged(IReadOnlyList<PetalChange> changes, BoardLayout boardLayout, float duration, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        var tasks = new List<UniTask>();

        foreach (PetalChange change in changes)
        {
            PetalView view = GetAccessibleView(accessKeys, change.Position);
            tasks.Add(PetalViewAnimator.PlayPetalChange(view, change.After, boardLayout.TileSize, duration));
        }

        await UniTask.WhenAll(tasks);
    }

    public async UniTask OnSwap(Vector2Int tileA, Vector2Int tileB, float tileSize, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        PetalView viewA = GetAccessibleView(accessKeys, tileA);
        PetalView viewB = GetAccessibleView(accessKeys, tileB);

        await UniTask.WhenAll(
            PetalViewAnimator.PlaySwap(viewA, viewB.transform.position, tileSize),
            PetalViewAnimator.PlaySwap(viewB, viewA.transform.position, tileSize)
        );

        petalViews[tileA.x, tileA.y] = viewB;
        petalViews[tileB.x, tileB.y] = viewA;
    }

    public UniTask PlayNormalRemovals(IReadOnlyList<Vector2Int> positions, IDictionary<Vector2Int, ViewAccessKey> accessKeys, float duration = DefaultDisappearDuration, Ease ease = Ease.InBack)
    {
        return ClearPetalViews(positions, duration, ease, accessKeys);
    }

    public void ReleasePetalViewsImmediately(IReadOnlyList<Vector2Int> positions, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        foreach (Vector2Int position in positions)
        {
            PetalView view = GetAccessibleView(accessKeys, position);
            ViewAccessKey accessKey = accessKeys[position];
            petalViews[position.x, position.y] = null;
            ReleaseView(accessKey);
            accessKeys.Remove(position);
            pool.Release(view);
        }
    }

    private async UniTask ClearPetalViews(IReadOnlyList<Vector2Int> cleared, float duration, Ease ease, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        var tasks = new List<UniTask>();
        foreach (Vector2Int tile in cleared)
        {
            PetalView view = GetAccessibleView(accessKeys, tile);
            ViewAccessKey accessKey = accessKeys[tile];
            petalViews[tile.x, tile.y] = null;
            tasks.Add(DestroyAndRelease(view, tile, duration, ease, accessKey, accessKeys));
        }
        await UniTask.WhenAll(tasks);
    }

    private async UniTask DestroyAndRelease(PetalView view, Vector2Int position, float duration, Ease ease, ViewAccessKey accessKey, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        await PetalViewAnimator.PlayDisappear(view, duration, ease);
        ReleaseView(accessKey);
        accessKeys.Remove(position);
        pool.Release(view);
    }

    public async UniTask PlayDisappearAndRelease(IReadOnlyList<Vector2Int> positions, float duration, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        var tasks = new List<UniTask>();

        foreach (Vector2Int position in positions)
        {
            PetalView view = GetAccessibleView(accessKeys, position);
            ViewAccessKey accessKey = accessKeys[position];
            petalViews[position.x, position.y] = null;
            tasks.Add(DisappearAndRelease(view, position, duration, accessKey, accessKeys));
        }

        await UniTask.WhenAll(tasks);
    }

    public UniTask PlayScale(Vector2Int position, float scaleMultiplier, float duration, Ease ease, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        PetalView view = GetAccessibleView(accessKeys, position);
        return PetalViewAnimator.PlayScale(view, scaleMultiplier, duration, ease);
    }

    public UniTask PlayRootScale(Vector2Int position, float scaleMultiplier, float duration, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        PetalView view = GetAccessibleView(accessKeys, position);
        return PetalViewAnimator.PlayRootScale(view, scaleMultiplier, duration);
    }

    public UniTask PlayAboutToExecuteShake(IReadOnlyList<Vector2Int> positions, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        foreach (Vector2Int position in positions)
        {
            PetalView view = GetAccessibleView(accessKeys, position);
            PetalViewAnimator.PlayAboutToExecute(view);
        }

        return UniTask.CompletedTask;
    }
    
    private async UniTask DisappearAndRelease(PetalView view, Vector2Int position, float duration, ViewAccessKey accessKey, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        await PetalViewAnimator.PlayDisappear(view, duration);
        view.transform.localRotation = Quaternion.identity;
        ReleaseView(accessKey);
        accessKeys.Remove(position);
        pool.Release(view);
    }

    public UniTask PlayFly(Vector2Int sourceTile, Vector2Int targetTile, BoardLayout boardLayout, float duration, float curveAmplitudeInTiles, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        PetalView view = GetAccessibleView(accessKeys, sourceTile);
        Vector2 targetWorldPosition = boardLayout.GetTileWorldPos(targetTile.x, targetTile.y);
        return PetalViewAnimator.PlayFly(view, targetWorldPosition, boardLayout.TileSize, duration, curveAmplitudeInTiles);
    }

    public Transform GetAccessibleVisualTransform(Vector2Int position, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        return GetAccessibleView(accessKeys, position).VisualTransform;
    }
    
    public async UniTask PlaySkillPetalCreations(IReadOnlyList<SkillPetalSpawn> spawns, BoardLayout boardLayout, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        var tasks = new List<UniTask>();

        foreach (SkillPetalSpawn spawn in spawns)
            tasks.Add(PlaySkillPetalCreation(spawn, boardLayout, accessKeys));

        await UniTask.WhenAll(tasks);
    }

    private async UniTask PlaySkillPetalCreation(SkillPetalSpawn spawn, BoardLayout boardLayout, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        Vector2 spawnWorldPosition = boardLayout.GetTileWorldPos(spawn.SpawnPosition.x, spawn.SpawnPosition.y);
        var contributors = new List<(Vector2Int position, PetalView view, ViewAccessKey accessKey)>(spawn.ContributorPositions.Count);
        var tasks = new List<UniTask>(spawn.ContributorPositions.Count);

        foreach (Vector2Int position in spawn.ContributorPositions)
        {
            PetalView contributorView = GetAccessibleView(accessKeys, position);
            petalViews[position.x, position.y] = null;
            contributors.Add((position, contributorView, accessKeys[position]));
            tasks.Add(PetalViewAnimator.PlayGather(contributorView, spawnWorldPosition));
        }

        await UniTask.WhenAll(tasks);

        foreach (var contributor in contributors)
        {
            ReleaseView(contributor.accessKey);
            accessKeys.Remove(contributor.position);
            pool.Release(contributor.view);
        }

        PetalView skillPetalView = pool.Get();
        skillPetalView.transform.position = spawnWorldPosition;
        skillPetalView.Init(new Petal(spawn.PetalType, spawn.SkillType), boardLayout.TileSize);
        Color color = skillPetalView.spriteRenderer.color;
        color.a = 1f;
        skillPetalView.spriteRenderer.color = color;
        petalViews[spawn.SpawnPosition.x, spawn.SpawnPosition.y] = skillPetalView;
    }

    public UniTask PlayComboMerge(Vector2Int sourceA, Vector2Int sourceB, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        PetalView viewA = GetAccessibleView(accessKeys, sourceA);
        PetalView viewB = GetAccessibleView(accessKeys, sourceB);
        return PetalViewAnimator.PlayComboMerge(viewA, viewB);
    }

    public async UniTask PlayComboSpinAndRelease(Vector2Int sourceA, Vector2Int sourceB, float duration, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        //TODO: bug, when sun burst dont get executed from a swap, it wouldnt have 2 view. count for that case too
        PetalView viewA = GetAccessibleView(accessKeys, sourceA);
        PetalView viewB = GetAccessibleView(accessKeys, sourceB);
        ViewAccessKey accessKeyA = accessKeys[sourceA];
        ViewAccessKey accessKeyB = accessKeys[sourceB];

        await PetalViewAnimator.PlayComboSpinAndDisappear(viewA, viewB, duration);

        petalViews[sourceA.x, sourceA.y] = null;
        petalViews[sourceB.x, sourceB.y] = null;
        ReleaseView(accessKeyA);
        ReleaseView(accessKeyB);
        accessKeys.Remove(sourceA);
        accessKeys.Remove(sourceB);
        pool.Release(viewA);
        pool.Release(viewB);
    }

    public async UniTask OnGravityApplied(List<(Vector2Int from, Vector2Int to)> moves, BoardLayout boardLayout, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        var tasks = new List<UniTask>();
        foreach (var (from, to) in moves)
        {
            PetalView view = GetAccessibleView(accessKeys, from);
            Vector2 targetPos = boardLayout.GetTileWorldPos(to.x, to.y);
            petalViews[to.x, to.y] = view;
            petalViews[from.x, from.y] = null;
            tasks.Add(PetalViewAnimator.PlayDrop(view, targetPos, boardLayout.TileSize));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnFilled(List<Vector2Int> filledTiles, BoardLayout boardLayout, Tile[,] grid, string userName, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        filledTiles.Sort((a, b) => a.y.CompareTo(b.y));
        var tasks = new List<UniTask>();
        foreach (Vector2Int tile in filledTiles)
        {
            Vector2 targetPos = boardLayout.GetTileWorldPos(tile.x, tile.y);
            Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.Rows * boardLayout.TileSize);
            PetalView view = pool.Get();
            view.transform.position = spawnPos;
            view.Init(grid[tile.x, tile.y].Petal, boardLayout.TileSize);
            petalViews[tile.x, tile.y] = view;
            RegisterViewAccess(view, tile, userName, accessKeys);
            PetalViewAnimator.PlaySpawn(view).Forget();
            tasks.Add(PetalViewAnimator.PlayDrop(view, targetPos, boardLayout.TileSize));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnShuffled(List<Vector2Int> tiles, BoardLayout boardLayout, Tile[,] grid, string userName, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        var tasks = new List<UniTask>();
        foreach (Vector2Int tile in tiles)
        {
            PetalView view = GetAccessibleView(accessKeys, tile);
            tasks.Add(ShuffleTile(view, tile, tile.x * 0.05f, boardLayout, grid, userName, accessKeys));
        }
        await UniTask.WhenAll(tasks);
    }
    
    private async UniTask ShuffleTile(PetalView view, Vector2Int tile, float delay, BoardLayout boardLayout, Tile[,] grid, string userName, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        await PetalViewAnimator.PlayDisappear(view, DefaultDisappearDuration, Ease.InBack, delay);
        ReleaseView(accessKeys[tile]);
        accessKeys.Remove(tile);
        pool.Release(view);

        Vector2 targetPos = boardLayout.GetTileWorldPos(tile.x, tile.y);
        Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.Rows * boardLayout.TileSize);
        PetalView newView = pool.Get();
        newView.transform.position = spawnPos;
        newView.Init(grid[tile.x, tile.y].Petal, boardLayout.TileSize);
        petalViews[tile.x, tile.y] = newView;
        RegisterViewAccess(newView, tile, userName, accessKeys);
        await PetalViewAnimator.PlaySpawn(newView);
        await PetalViewAnimator.PlayDrop(newView, targetPos, boardLayout.TileSize);
    }
    
    public void RefreshTile(Vector2Int tile, Petal petal, BoardLayout boardLayout, string userName, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        PetalView existing = petalViews[tile.x, tile.y];
        if (existing != null)
        {
            GetAccessibleView(accessKeys, tile);
            ReleaseView(accessKeys[tile]);
            accessKeys.Remove(tile);
            pool.Release(existing);
            petalViews[tile.x, tile.y] = null;
        }

        if (petal == null) return;

        Vector2 pos = boardLayout.GetTileWorldPos(tile.x, tile.y);
        PetalView view = pool.Get();
        view.transform.position = pos;
        view.Init(petal, boardLayout.TileSize);
        petalViews[tile.x, tile.y] = view;
        RegisterViewAccess(view, tile, userName, accessKeys);
    }

    private void RegisterViewAccess(PetalView view, Vector2Int position, string userName, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
    {
        if (activeUses.ContainsKey(view))
            throw new InvalidOperationException($"Petal view at {position} is already being used.");
        if (accessKeys.ContainsKey(position))
            throw new InvalidOperationException($"Petal view access at {position} is already registered for '{accessKeys[position].UserName}'.");

        var accessKey = new ViewAccessKey(view, position, userName);
        activeUses.Add(view, accessKey);
        accessKeys.Add(position, accessKey);
    }

    private PetalView GetAccessibleView(IDictionary<Vector2Int, ViewAccessKey> accessKeys, Vector2Int position)
    {
        if (!accessKeys.TryGetValue(position, out ViewAccessKey accessKey))
            throw new InvalidOperationException($"Petal view at {position} has no access key.");
        if (!activeUses.TryGetValue(accessKey.View, out ViewAccessKey currentAccessKey) || currentAccessKey != accessKey)
            throw new InvalidOperationException($"Petal view access held by '{accessKey.UserName}' is not active.");
        if (accessKey.Position != position)
            throw new InvalidOperationException($"Petal view access held by '{accessKey.UserName}' belongs to {accessKey.Position}, not {position}.");
        if (petalViews[position.x, position.y] != accessKey.View)
            throw new InvalidOperationException($"Petal view at {position} no longer matches the view held by '{accessKey.UserName}'.");
        return accessKey.View;
    }
}
