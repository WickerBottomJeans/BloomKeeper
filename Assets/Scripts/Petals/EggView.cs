using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DG.Tweening;
using UnityEngine;

public sealed class EggView : PieceView
{
    [SerializeField] private Sprite eggSprite;

    public override void Refresh(IBoardPiece piece, float tileSize)
    {
        if (piece is not Egg) throw new ArgumentException("An egg view requires an egg.", nameof(piece));
        RefreshSprite(eggSprite, tileSize);
    }

    public async UniTask PlayCollection(Vector3 destination, float tileSize, CancellationToken cancellationToken)
    {
        KillActiveAnimation();
        spriteRenderer.sortingOrder += 20;
        SyncShadowSorting();
        Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy).SetTarget(transform);
        sequence.Append(transform.DOJump(destination, tileSize * 0.45f, 1, 0.45f).SetEase(Ease.InOutQuad));
        sequence.Insert(0.25f, VisualTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
        // Finish killing before the presenter resumes and resets this view for its pool.
        bool canceled = await sequence.ToUniTask(TweenCancelBehaviour.CancelAwait, cancellationToken).SuppressCancellationThrow();
        if (canceled)
        {
            sequence.Kill();
            throw new OperationCanceledException(cancellationToken);
        }
    }
}
