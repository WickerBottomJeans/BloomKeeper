using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

public class PetalViewManager : MonoBehaviour
{
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

                Vector2 pos = boardLayout.GetTileWorldPosition(x, y);
                PetalView view = pool.Get();
                view.transform.position = pos;
                view.Refresh(tile.Petal, boardLayout.TileSize);
                petalViews[x, y] = view;
            }
        }
    }

    internal bool TryAcquireView(Vector2Int position, PetalViewScope owner, out ViewAccessKey accessKey)
    {
        PetalView view = petalViews[position.x, position.y];
        if (view == null || activeUses.ContainsKey(view))
        {
            accessKey = null;
            return false;
        }

        accessKey = new ViewAccessKey(view, position, owner);
        activeUses.Add(view, accessKey);
        owner.Add(position, accessKey);
        return true;
    }

    internal void ReleaseView(ViewAccessKey accessKey)
    {
        if (!activeUses.TryGetValue(accessKey.View, out ViewAccessKey currentAccessKey))
            throw new InvalidOperationException($"Petal view access held by '{accessKey.UserName}' is not active.");
        if (currentAccessKey != accessKey)
            throw new InvalidOperationException($"Petal view access held by '{accessKey.UserName}' does not match the active access key.");
        activeUses.Remove(accessKey.View);
        accessKey.Owner.Remove(accessKey.Position);
    }

    internal void TransferView(Vector2Int position, PetalViewScope owner)
    {
        PetalView view = petalViews[position.x, position.y];
        if (view != null && activeUses.TryGetValue(view, out ViewAccessKey key))
        {
            if (key.Owner == owner) return;
            ReleaseView(key);
        }
        owner.AcquireRequired(position);
    }

    public UniTask PlayPetalChange(PetalChange change, BoardLayout boardLayout, PetalViewScope views, float duration = 0.1f)
    {
        PetalView view = GetAccessibleView(views, change.Position);
        return view.PlayPetalChange(change.After, boardLayout.TileSize, duration);
    }

    public void ApplyPetalChangeImmediately(PetalChange change, BoardLayout boardLayout, PetalViewScope views)
    {
        PetalView view = GetAccessibleView(views, change.Position);
        view.Refresh(change.After, boardLayout.TileSize);
    }

    public async UniTask OnSwap(Vector2Int tileA, Vector2Int tileB, float tileSize, PetalViewScope views)
    {
        PetalView viewA = GetAccessibleView(views, tileA);
        PetalView viewB = GetAccessibleView(views, tileB);

        await UniTask.WhenAll(
            viewA.PlaySwap(viewB.transform.position, tileSize),
            viewB.PlaySwap(viewA.transform.position, tileSize)
        );

        petalViews[tileA.x, tileA.y] = viewB;
        petalViews[tileB.x, tileB.y] = viewA;
    }

    public UniTask PlayNormalRemovals(IReadOnlyList<Vector2Int> positions, PetalViewScope views, float duration = 0.15f, Ease ease = Ease.InBack)
    {
        return ClearPetalViews(positions, duration, ease, views);
    }

    public void ReleasePetalViewsImmediately(IReadOnlyList<Vector2Int> positions, PetalViewScope views)
    {
        foreach (Vector2Int position in positions)
        {
            PetalView view = GetAccessibleView(views, position);
            ViewAccessKey accessKey = views[position];
            petalViews[position.x, position.y] = null;
            ReleaseView(accessKey);
            pool.Release(view);
        }
    }

    private async UniTask ClearPetalViews(IReadOnlyList<Vector2Int> cleared, float duration, Ease ease, PetalViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (Vector2Int tile in cleared)
        {
            PetalView view = GetAccessibleView(views, tile);
            ViewAccessKey accessKey = views[tile];
            petalViews[tile.x, tile.y] = null;
            tasks.Add(DestroyAndRelease(view, tile, duration, ease, accessKey, views));
        }
        await UniTask.WhenAll(tasks);
    }

    private async UniTask DestroyAndRelease(PetalView view, Vector2Int position, float duration, Ease ease, ViewAccessKey accessKey, PetalViewScope views)
    {
        await view.PlayDisappear(duration, ease);
        ReleaseView(accessKey);
        pool.Release(view);
    }

    public async UniTask PlayDisappearAndRelease(IReadOnlyList<Vector2Int> positions, float duration, PetalViewScope views)
    {
        var tasks = new List<UniTask>();

        foreach (Vector2Int position in positions)
        {
            PetalView view = GetAccessibleView(views, position);
            ViewAccessKey accessKey = views[position];
            petalViews[position.x, position.y] = null;
            tasks.Add(DisappearAndRelease(view, position, duration, accessKey, views));
        }

        await UniTask.WhenAll(tasks);
    }

    public UniTask PlayScale(Vector2Int position, float scaleMultiplier, float duration, Ease ease, PetalViewScope views)
    {
        PetalView view = GetAccessibleView(views, position);
        return view.PlayScale(scaleMultiplier, duration, ease);
    }

    public void ShowBoosterSelection(Vector2Int position, PetalViewScope views)
    {
        GetAccessibleView(views, position).ShowBoosterSelection();
    }

    public void HideBoosterSelection(Vector2Int position, PetalViewScope views)
    {
        GetAccessibleView(views, position).HideBoosterSelection();
    }

    public UniTask PlayRootScale(Vector2Int position, float scaleMultiplier, float duration, PetalViewScope views)
    {
        PetalView view = GetAccessibleView(views, position);
        return view.PlayRootScale(scaleMultiplier, duration);
    }

    public UniTask PlayPrismaticBloomPrepareSpin(Vector2Int position, float duration, float maximumSpinSpeed, PetalViewScope views)
    {
        PetalView view = GetAccessibleView(views, position);
        return view.PlayPrismaticBloomPrepareSpin(duration, maximumSpinSpeed);
    }

    public UniTask PlayPrismaticBloomFireSpin(Vector2Int position, float duration, float maximumSpinSpeed, PetalViewScope views)
    {
        PetalView view = GetAccessibleView(views, position);
        return view.PlayPrismaticBloomFireSpin(duration, maximumSpinSpeed);
    }

    public UniTask PlayBubbleInflate(Vector2Int position, float scaleMultiplier, float duration, PetalViewScope views)
    {
        return GetAccessibleView(views, position).PlayBubbleInflate(scaleMultiplier, duration);
    }

    public void HideBubbleForPop(Vector2Int position, PetalViewScope views)
    {
        GetAccessibleView(views, position).SetBubbleVisible(false);
    }

    public UniTask PlayAboutToExecute(IReadOnlyList<Vector2Int> positions, PetalViewScope views)
    {
        foreach (Vector2Int position in positions)
        {
            PetalView view = GetAccessibleView(views, position);
            view.PlayAboutToExecute();
        }

        return UniTask.CompletedTask;
    }
    
    private async UniTask DisappearAndRelease(PetalView view, Vector2Int position, float duration, ViewAccessKey accessKey, PetalViewScope views)
    {
        await view.PlayDisappear(duration);
        view.transform.localRotation = Quaternion.identity;
        ReleaseView(accessKey);
        pool.Release(view);
    }

    public UniTask PlayFly(Vector2Int sourceTile, Vector2Int targetTile, BoardLayout boardLayout, float duration, float curveAmplitudeInTiles, PetalViewScope views)
    {
        PetalView view = GetAccessibleView(views, sourceTile);
        Vector2 targetWorldPosition = boardLayout.GetTileWorldPosition(targetTile.x, targetTile.y);
        return view.PlayFly(targetWorldPosition, boardLayout.TileSize, duration, curveAmplitudeInTiles);
    }

    public Transform GetAccessibleVisualTransform(Vector2Int position, PetalViewScope views)
    {
        return GetAccessibleView(views, position).VisualTransform;
    }
    
    public async UniTask PlaySkillPetalCreations(IReadOnlyList<SkillPetalSpawn> spawns, BoardLayout boardLayout, PetalViewScope views)
    {
        var tasks = new List<UniTask>();

        foreach (SkillPetalSpawn spawn in spawns)
            tasks.Add(PlaySkillPetalCreation(spawn, boardLayout, views));

        await UniTask.WhenAll(tasks);
    }

    private async UniTask PlaySkillPetalCreation(SkillPetalSpawn spawn, BoardLayout boardLayout, PetalViewScope views)
    {
        Vector2 spawnWorldPosition = boardLayout.GetTileWorldPosition(spawn.SpawnPosition.x, spawn.SpawnPosition.y);
        var contributors = new List<(Vector2Int position, PetalView view, ViewAccessKey accessKey)>(spawn.ContributorPositions.Count);
        var tasks = new List<UniTask>(spawn.ContributorPositions.Count);

        foreach (Vector2Int position in spawn.ContributorPositions)
        {
            PetalView contributorView = GetAccessibleView(views, position);
            petalViews[position.x, position.y] = null;
            contributors.Add((position, contributorView, views[position]));
            tasks.Add(contributorView.PlayGather(spawnWorldPosition));
        }

        await UniTask.WhenAll(tasks);

        foreach (var contributor in contributors)
        {
            ReleaseView(contributor.accessKey);
            pool.Release(contributor.view);
        }

        PetalView skillPetalView = pool.Get();
        skillPetalView.transform.position = spawnWorldPosition;
        skillPetalView.Refresh(new Petal(spawn.PetalType, spawn.SkillType), boardLayout.TileSize);
        Color color = skillPetalView.spriteRenderer.color;
        color.a = 1f;
        skillPetalView.spriteRenderer.color = color;
        petalViews[spawn.SpawnPosition.x, spawn.SpawnPosition.y] = skillPetalView;
    }
    

    public async UniTask OnGravityApplied(List<(Vector2Int from, Vector2Int to)> moves, BoardLayout boardLayout, PetalViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (var (from, to) in moves)
        {
            PetalView view = GetAccessibleView(views, from);
            Vector2 targetPos = boardLayout.GetTileWorldPosition(to.x, to.y);
            petalViews[to.x, to.y] = view;
            petalViews[from.x, from.y] = null;
            tasks.Add(view.PlayDrop(targetPos, boardLayout.TileSize));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnFilled(IReadOnlyList<(Vector2Int position, Petal petal)> filledTiles, BoardLayout boardLayout, PetalViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (var (tile, petal) in filledTiles)
        {
            Vector2 targetPos = boardLayout.GetTileWorldPosition(tile.x, tile.y);
            Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.RowCount * boardLayout.TileSize);
            PetalView view = pool.Get();
            view.transform.position = spawnPos;
            view.Refresh(petal, boardLayout.TileSize);
            petalViews[tile.x, tile.y] = view;
            RegisterViewAccess(view, tile, views);
            view.PlaySpawn().Forget();
            tasks.Add(view.PlayDrop(targetPos, boardLayout.TileSize));
        }
        await UniTask.WhenAll(tasks);
    }
    
    public async UniTask OnShuffled(IReadOnlyList<(Vector2Int position, Petal petal)> tiles, BoardLayout boardLayout, PetalViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (var (tile, petal) in tiles)
        {
            PetalView view = GetAccessibleView(views, tile);
            tasks.Add(ShuffleTile(view, tile, petal, tile.x * 0.05f, boardLayout, views));
        }
        await UniTask.WhenAll(tasks);
    }
    
    private async UniTask ShuffleTile(PetalView view, Vector2Int tile, Petal petal, float delay, BoardLayout boardLayout, PetalViewScope views)
    {
        await view.PlayDisappear(0.1f, Ease.InBack, delay);
        ReleaseView(views[tile]);
        pool.Release(view);

        Vector2 targetPos = boardLayout.GetTileWorldPosition(tile.x, tile.y);
        Vector2 spawnPos = new Vector2(targetPos.x, targetPos.y + boardLayout.RowCount * boardLayout.TileSize);
        PetalView newView = pool.Get();
        newView.transform.position = spawnPos;
        newView.Refresh(petal, boardLayout.TileSize);
        petalViews[tile.x, tile.y] = newView;
        RegisterViewAccess(newView, tile, views);
        await newView.PlaySpawn();
        await newView.PlayDrop(targetPos, boardLayout.TileSize);
    }
    
    public void RefreshTile(Vector2Int tile, Petal petal, BoardLayout boardLayout, PetalViewScope views)
    {
        PetalView existing = petalViews[tile.x, tile.y];
        if (existing != null)
        {
            GetAccessibleView(views, tile);
            ReleaseView(views[tile]);
            pool.Release(existing);
            petalViews[tile.x, tile.y] = null;
        }

        if (petal == null) return;

        Vector2 pos = boardLayout.GetTileWorldPosition(tile.x, tile.y);
        PetalView view = pool.Get();
        view.transform.position = pos;
        view.Refresh(petal, boardLayout.TileSize);
        petalViews[tile.x, tile.y] = view;
        RegisterViewAccess(view, tile, views);
    }

    private void RegisterViewAccess(PetalView view, Vector2Int position, PetalViewScope views)
    {
        if (activeUses.ContainsKey(view))
            throw new InvalidOperationException($"Petal view at {position} is already being used.");
        if (views.Owns(position))
            throw new InvalidOperationException($"Petal view access at {position} is already registered for '{views[position].UserName}'.");

        var accessKey = new ViewAccessKey(view, position, views);
        activeUses.Add(view, accessKey);
        views.Add(position, accessKey);
    }

    private PetalView GetAccessibleView(PetalViewScope views, Vector2Int position)
    {
        if (!views.TryGetValue(position, out ViewAccessKey accessKey))
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
