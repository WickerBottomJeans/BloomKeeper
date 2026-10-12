using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DG.Tweening;
using UnityEngine;

public static class PetalPresentation
{
    public static UniTask PlayPetalChange(PieceViewManager pieceViewManager, PetalChange change, BoardLayout boardLayout, PieceViewScope views, float duration = 0.1f)
    {
        PetalView view = pieceViewManager.GetRequiredView<PetalView>(views, change.Position);
        return view.PlayPetalChange(change.After, boardLayout.TileSize, duration);
    }

    public static void ApplyPetalChangeImmediately(PieceViewManager pieceViewManager, PetalChange change, BoardLayout boardLayout, PieceViewScope views)
    {
        PetalView view = pieceViewManager.GetRequiredView<PetalView>(views, change.Position);
        view.Refresh(change.After, boardLayout.TileSize);
    }

    public static UniTask PlayPrismaticBloomPrepareSpin(PieceViewManager pieceViewManager, Vector2Int position, float duration, float maximumSpinSpeed, PieceViewScope views)
    {
        PetalView view = pieceViewManager.GetRequiredView<PetalView>(views, position);
        return view.PlayPrismaticBloomPrepareSpin(duration, maximumSpinSpeed);
    }

    public static UniTask PlayPrismaticBloomFireSpin(PieceViewManager pieceViewManager, Vector2Int position, float duration, float maximumSpinSpeed, PieceViewScope views)
    {
        PetalView view = pieceViewManager.GetRequiredView<PetalView>(views, position);
        return view.PlayPrismaticBloomFireSpin(duration, maximumSpinSpeed);
    }

    public static UniTask PlayBubbleInflate(PieceViewManager pieceViewManager, Vector2Int position, float scaleMultiplier, float duration, PieceViewScope views)
    {
        return pieceViewManager.GetRequiredView<PetalView>(views, position).PlayBubbleInflate(scaleMultiplier, duration);
    }

    public static void HideBubbleForPop(PieceViewManager pieceViewManager, Vector2Int position, PieceViewScope views)
    {
        pieceViewManager.GetRequiredView<PetalView>(views, position).SetBubbleVisible(false);
    }

    public static UniTask PlayAboutToExecute(PieceViewManager pieceViewManager, IReadOnlyList<Vector2Int> positions, PieceViewScope views)
    {
        foreach (Vector2Int position in positions)
        {
            PetalView view = pieceViewManager.GetRequiredView<PetalView>(views, position);
            view.PlayAboutToExecute();
        }

        return UniTask.CompletedTask;
    }

    public static UniTask PlayFly(PieceViewManager pieceViewManager, Vector2Int sourceTile, Vector2Int targetTile, BoardLayout boardLayout, float duration, float curveAmplitudeInTiles, PieceViewScope views)
    {
        PetalView view = pieceViewManager.GetRequiredView<PetalView>(views, sourceTile);
        Vector2 targetWorldPosition = boardLayout.GetTileWorldPosition(targetTile.x, targetTile.y);
        return view.PlayFly(targetWorldPosition, boardLayout.TileSize, duration, curveAmplitudeInTiles);
    }

    public static async UniTask PlaySkillPetalCreations(PieceViewManager pieceViewManager, IReadOnlyList<SkillPetalSpawn> spawns, BoardLayout boardLayout, PieceViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (SkillPetalSpawn spawn in spawns) tasks.Add(PlaySkillPetalCreation(pieceViewManager, spawn, boardLayout, views));
        await UniTask.WhenAll(tasks);
    }

    private static async UniTask PlaySkillPetalCreation(PieceViewManager pieceViewManager, SkillPetalSpawn spawn, BoardLayout boardLayout, PieceViewScope views)
    {
        Vector2 destination = boardLayout.GetTileWorldPosition(spawn.SpawnPosition.x, spawn.SpawnPosition.y);
        var tasks = new List<UniTask>();
        foreach (Vector2Int position in spawn.ContributorPositions)
            tasks.Add(pieceViewManager.GetRequiredView<PetalView>(views, position).PlayGather(destination));
        await UniTask.WhenAll(tasks);
        foreach (Vector2Int position in spawn.ContributorPositions) pieceViewManager.RecycleView(position, views);
        pieceViewManager.CreateView(new Petal(spawn.PetalType, spawn.SkillType), spawn.SpawnPosition, destination, boardLayout);
    }

    public static async UniTask OnFilled(PieceViewManager pieceViewManager, IReadOnlyList<(Vector2Int position, Petal petal)> filledTiles, BoardLayout boardLayout, PieceViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (var (position, petal) in filledTiles)
        {
            Vector2 target = boardLayout.GetTileWorldPosition(position.x, position.y);
            Vector2 source = target + Vector2.up * boardLayout.RowCount * boardLayout.TileSize;
            PieceView view = pieceViewManager.CreateView(petal, position, source, boardLayout, views);
            view.PlaySpawn().Forget();
            tasks.Add(view.PlayDrop(target, boardLayout.TileSize));
        }
        await UniTask.WhenAll(tasks);
    }

    public static async UniTask OnShuffled(PieceViewManager pieceViewManager, IReadOnlyList<(Vector2Int position, Petal petal)> tiles, BoardLayout boardLayout, PieceViewScope views)
    {
        var tasks = new List<UniTask>();
        foreach (var (position, petal) in tiles) tasks.Add(ShuffleTile(pieceViewManager, position, petal, boardLayout, views));
        await UniTask.WhenAll(tasks);
    }

    private static async UniTask ShuffleTile(PieceViewManager pieceViewManager, Vector2Int position, Petal petal, BoardLayout boardLayout, PieceViewScope views)
    {
        PetalView previousView = pieceViewManager.GetRequiredView<PetalView>(views, position);
        await previousView.PlayDisappear(0.1f, Ease.InBack, position.x * 0.05f);
        pieceViewManager.RecycleView(position, views);
        Vector2 target = boardLayout.GetTileWorldPosition(position.x, position.y);
        PieceView view = pieceViewManager.CreateView(petal, position, target + Vector2.up * boardLayout.RowCount * boardLayout.TileSize, boardLayout, views);
        await view.PlaySpawn();
        await view.PlayDrop(target, boardLayout.TileSize);
    }

    public static void RefreshTile(PieceViewManager pieceViewManager, Vector2Int position, Petal petal, BoardLayout boardLayout, PieceViewScope views)
    {
        if (views.Owns(position)) pieceViewManager.RecycleView(position, views);
        if (petal != null) pieceViewManager.CreateView(petal, position, boardLayout.GetTileWorldPosition(position.x, position.y), boardLayout, views);
    }
}
