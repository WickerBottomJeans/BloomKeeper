using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public sealed class BasketFeatureView : TileFeatureView
    {
        [SerializeField] private SpriteRenderer basketRenderer;
        [SerializeField] private Transform receivingPoint;
        public override TileFeatureType FeatureType => TileFeatureType.Basket;
        public Vector3 ReceivingPosition => receivingPoint.position;

        public override void InitializeFeatureView(float tileSize)
        {
            Vector2 size = basketRenderer.sprite.bounds.size;
            transform.localScale = Vector3.one * (tileSize * 0.95f / Mathf.Max(size.x, size.y));
        }

        public override void DisplayFeatureState(TileFeatureState tileFeatureState)
        {
            if (tileFeatureState is not BasketFeatureState) throw new ArgumentException("A basket view requires a basket snapshot.", nameof(tileFeatureState));
        }

        public UniTask PlayReceive(CancellationToken cancellationToken)
        {
            return basketRenderer.transform.DOPunchScale(new Vector3(0.12f, -0.08f, 0f), 0.2f, 1).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
        }

        public override UniTask PlayFeatureSpawn() => UniTask.CompletedTask;
        public override UniTask PlayFeatureChange(TileFeatureState tileFeatureState) { DisplayFeatureState(tileFeatureState); return UniTask.CompletedTask; }
        public override UniTask PlayFeatureRemoval() => UniTask.CompletedTask;
    }
}
