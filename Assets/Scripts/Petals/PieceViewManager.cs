using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Owns one piece-view registry, exclusive access, concrete view pools, and shared movement. Flower behavior lives in PetalPresentation.</summary>
public class PieceViewManager : MonoBehaviour
{
    //TODO: should this cache board layout, the layout dont really channge over time
    [SerializeField] private PetalView petalViewPrefab;

    [SerializeField] private EggView eggViewPrefab;

    private PieceView[,] pieceViews;
    private readonly Dictionary<Type, ObjectPool<PieceView>> pools = new();
    private readonly Dictionary<PieceView, ViewAccessKey> activeUses = new Dictionary<PieceView, ViewAccessKey>();

    public void Init(Tile[,] grid, BoardLayout boardLayout)
    {
        RegisterPool(petalViewPrefab);
        RegisterPool(eggViewPrefab);
        pieceViews = new PieceView[grid.GetLength(0), grid.GetLength(1)];
        for (int x = 0; x < grid.GetLength(0); x++)
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            IBoardPiece piece = grid[x, y]?.Piece;
            if (piece == null) continue;
            var position = new Vector2Int(x, y);
            CreateView(piece, position, boardLayout.GetTileWorldPosition(x, y), boardLayout);
        }
    }

    private void RegisterPool(PieceView prefab)
    {
        if (prefab == null) throw new InvalidOperationException("A piece view prefab is missing.");
        pools.Add(prefab.GetType(), new ObjectPool<PieceView>(() => Instantiate(prefab, transform), view => view.gameObject.SetActive(true), view => view.gameObject.SetActive(false), view => Destroy(view.gameObject)));
    }

    internal PieceView CreateView(IBoardPiece piece, Vector2Int position, Vector3 worldPosition, BoardLayout boardLayout, PieceViewScope views = null)
    {
        if (pieceViews[position.x, position.y] != null) throw new InvalidOperationException($"Piece view slot {position} is occupied.");
        Type viewType = piece switch { Petal => typeof(PetalView), Egg => typeof(EggView), _ => throw new ArgumentException("Unsupported board piece.", nameof(piece)) };
        PieceView view = pools[viewType].Get();
        view.transform.position = worldPosition;
        view.Refresh(piece, boardLayout.TileSize);
        pieceViews[position.x, position.y] = view;
        if (views != null) RegisterViewAccess(view, position, views);
        return view;
    }

    internal void RecycleView(Vector2Int position, PieceViewScope views)
    {
        PieceView view = GetAccessibleView(views, position);
        pieceViews[position.x, position.y] = null;
        ReturnToPool(view, views[position]);
    }

    private void ReturnToPool(PieceView view, ViewAccessKey accessKey)
    {
        if (view != null) view.ResetForPool();
        ReleaseView(accessKey);
        if (view != null) pools[view.GetType()].Release(view);
    }

    internal TView GetRequiredView<TView>(PieceViewScope views, Vector2Int position) where TView : PieceView
    {
        if (GetAccessibleView(views, position) is not TView view) throw new InvalidOperationException($"Piece at {position} requires a {typeof(TView).Name} view.");
        return view;
    }

    internal bool TryAcquireView(Vector2Int position, PieceViewScope owner, out ViewAccessKey accessKey)
    {
        PieceView view = pieceViews[position.x, position.y];
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
            throw new InvalidOperationException($"Piece view access held by '{accessKey.UserName}' is not active.");
        if (currentAccessKey != accessKey)
            throw new InvalidOperationException($"Piece view access held by '{accessKey.UserName}' does not match the active access key.");
        activeUses.Remove(accessKey.View);
        accessKey.Owner.Remove(accessKey.Position);
    }

    internal void TransferView(Vector2Int position, PieceViewScope owner)
    {
        PieceView view = pieceViews[position.x, position.y];
        if (view != null && activeUses.TryGetValue(view, out ViewAccessKey key))
        {
            if (key.Owner == owner) return;
            ReleaseView(key);
        }
        owner.AcquireRequired(position);
    }

    public async UniTask OnSwap(Vector2Int tileA, Vector2Int tileB, float tileSize, PieceViewScope views)
    {
        PieceView viewA = GetAccessibleView(views, tileA);
        PieceView viewB = GetAccessibleView(views, tileB);

        await UniTask.WhenAll(
            viewA.PlaySwap(viewB.transform.position, tileSize),
            viewB.PlaySwap(viewA.transform.position, tileSize)
        );

        pieceViews[tileA.x, tileA.y] = viewB;
        pieceViews[tileB.x, tileB.y] = viewA;
    }

    public UniTask PlayNormalRemovals(IReadOnlyList<Vector2Int> positions, PieceViewScope views, float duration = 0.15f, Ease ease = Ease.InBack)
    {
        return ClearPieceViews(positions, duration, ease, views);
    }

    public void ReleasePieceViewsImmediately(IReadOnlyList<Vector2Int> positions, PieceViewScope views)
    {
        foreach (Vector2Int position in positions)
        {
            PieceView view = GetAccessibleView(views, position);
            ViewAccessKey accessKey = views[position];
            pieceViews[position.x, position.y] = null;
            ReturnToPool(view, accessKey);
        }
    }

    private async UniTask ClearPieceViews(IReadOnlyList<Vector2Int> cleared, float duration, Ease ease, PieceViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (Vector2Int tile in cleared)
        {
            PieceView view = GetAccessibleView(views, tile);
            ViewAccessKey accessKey = views[tile];
            pieceViews[tile.x, tile.y] = null;
            tasks.Add(DestroyAndRelease(view, duration, ease, accessKey));
        }
        await UniTask.WhenAll(tasks);
    }

    private async UniTask DestroyAndRelease(PieceView view, float duration, Ease ease, ViewAccessKey accessKey)
    {
        try { await view.PlayDisappear(duration, ease); }
        finally { ReturnToPool(view, accessKey); }
    }

    public async UniTask PlayDisappearAndRelease(IReadOnlyList<Vector2Int> positions, float duration, PieceViewScope views)
    {
        var tasks = new List<UniTask>();

        foreach (Vector2Int position in positions)
        {
            PieceView view = GetAccessibleView(views, position);
            ViewAccessKey accessKey = views[position];
            pieceViews[position.x, position.y] = null;
            tasks.Add(DisappearAndRelease(view, duration, accessKey));
        }

        await UniTask.WhenAll(tasks);
    }

    public UniTask PlayScale(Vector2Int position, float scaleMultiplier, float duration, Ease ease, PieceViewScope views)
    {
        PieceView view = GetAccessibleView(views, position);
        return view.PlayScale(scaleMultiplier, duration, ease);
    }

    public void ShowBoosterSelection(Vector2Int position, PieceViewScope views)
    {
        GetAccessibleView(views, position).ShowBoosterSelection();
    }

    public void HideBoosterSelection(Vector2Int position, PieceViewScope views)
    {
        GetAccessibleView(views, position).HideBoosterSelection();
    }

    public UniTask PlayRootScale(Vector2Int position, float scaleMultiplier, float duration, PieceViewScope views)
    {
        PieceView view = GetAccessibleView(views, position);
        return view.PlayRootScale(scaleMultiplier, duration);
    }

    private async UniTask DisappearAndRelease(PieceView view, float duration, ViewAccessKey accessKey)
    {
        try { await view.PlayDisappear(duration); }
        finally { ReturnToPool(view, accessKey); }
    }

    public Transform GetAccessibleVisualTransform(Vector2Int position, PieceViewScope views)
    {
        return GetAccessibleView(views, position).VisualTransform;
    }

    public async UniTask OnGravityApplied(List<(Vector2Int from, Vector2Int to)> moves, BoardLayout boardLayout, PieceViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (var (from, to) in moves)
        {
            PieceView view = GetAccessibleView(views, from);
            Vector2 targetPos = boardLayout.GetTileWorldPosition(to.x, to.y);
            pieceViews[to.x, to.y] = view;
            pieceViews[from.x, from.y] = null;
            tasks.Add(view.PlayDrop(targetPos, boardLayout.TileSize));
        }
        await UniTask.WhenAll(tasks);
    }

    private void RegisterViewAccess(PieceView view, Vector2Int position, PieceViewScope views)
    {
        if (activeUses.ContainsKey(view))
            throw new InvalidOperationException($"Piece view at {position} is already being used.");
        if (views.Owns(position))
            throw new InvalidOperationException($"Piece view access at {position} is already registered for '{views[position].UserName}'.");

        var accessKey = new ViewAccessKey(view, position, views);
        activeUses.Add(view, accessKey);
        views.Add(position, accessKey);
    }

    private PieceView GetAccessibleView(PieceViewScope views, Vector2Int position)
    {
        if (!views.TryGetValue(position, out ViewAccessKey accessKey))
            throw new InvalidOperationException($"Piece view at {position} has no access key.");
        if (!activeUses.TryGetValue(accessKey.View, out ViewAccessKey currentAccessKey) || currentAccessKey != accessKey)
            throw new InvalidOperationException($"Piece view access held by '{accessKey.UserName}' is not active.");
        if (accessKey.Position != position)
            throw new InvalidOperationException($"Piece view access held by '{accessKey.UserName}' belongs to {accessKey.Position}, not {position}.");
        if (pieceViews[position.x, position.y] != accessKey.View)
            throw new InvalidOperationException($"Piece view at {position} no longer matches the view held by '{accessKey.UserName}'.");
        return accessKey.View;
    }
}
