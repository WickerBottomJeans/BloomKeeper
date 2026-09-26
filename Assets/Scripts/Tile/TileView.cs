using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private Sprite playableTileSprite;
        [SerializeField] private Sprite inactiveTileSprite;

        private float tileSize;
        private Vector3 restingLocalPosition;
        private Material normalBaseMaterial;
        private bool boosterTargetShown;
        private TileFeatureView tileFeatureViewInstance;
        private IReadOnlyDictionary<TileFeatureType, TileFeatureView> tileFeaturePrefabsByType;

        #region Public API
        public void Init(float tileSize, int baseSortingOrder, bool isPlayable, TileFeatureState tileFeatureState, IReadOnlyDictionary<TileFeatureType, TileFeatureView> tileFeaturePrefabsByType)
        {
            if (tileSize <= 0f) throw new ArgumentOutOfRangeException(nameof(tileSize));

            this.tileSize = tileSize;
            this.tileFeaturePrefabsByType = tileFeaturePrefabsByType;
            restingLocalPosition = transform.localPosition;
            normalBaseMaterial = _baseRenderer.sharedMaterial;
            _baseRenderer.sortingOrder = baseSortingOrder;
            Sprite tileSprite = isPlayable ? playableTileSprite : inactiveTileSprite;
            _baseRenderer.sprite = tileSprite;
            _baseRenderer.transform.localScale = Vector3.one * (tileSize / tileSprite.bounds.size.x);
            if (tileFeatureState != null) SpawnTileFeatureView(tileFeatureState);
        }

        public async UniTask PlayTileFeatureChange(TileFeatureState beforeFeatureState, TileFeatureState afterFeatureState)
        {
            if (beforeFeatureState != null && (afterFeatureState == null || beforeFeatureState.FeatureType != afterFeatureState.FeatureType))
            {
                await tileFeatureViewInstance.PlayFeatureRemoval();
                Destroy(tileFeatureViewInstance.gameObject);
                tileFeatureViewInstance = null;
            }

            if (afterFeatureState == null) return;

            if (tileFeatureViewInstance == null)
            {
                SpawnTileFeatureView(afterFeatureState);
                await tileFeatureViewInstance.PlayFeatureSpawn();
            }
            else
            {
                await tileFeatureViewInstance.PlayFeatureChange(afterFeatureState);
            }
        }

        public void ShowBoosterTarget(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (boosterTargetShown) throw new InvalidOperationException("Booster target presentation is already shown on this tile.");

            boosterTargetShown = true;
            _baseRenderer.sharedMaterial = material;
        }

        public void HideBoosterTarget()
        {
            if (!boosterTargetShown) throw new InvalidOperationException("Booster target presentation is not shown on this tile.");

            _baseRenderer.sharedMaterial = normalBaseMaterial;
            boosterTargetShown = false;
        }

        public UniTask PlayRipple(Vector2 displacement, float delay, float duration)
        {
            if (delay < 0f) throw new ArgumentOutOfRangeException(nameof(delay));
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));

            transform.DOKill();
            transform.localPosition = restingLocalPosition;

            float legDuration = duration * 0.5f;
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.SetTarget(transform);
            sequence.AppendInterval(delay);
            sequence.Append(transform.DOLocalMove(restingLocalPosition + (Vector3)displacement, legDuration).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOLocalMove(restingLocalPosition, legDuration).SetEase(Ease.OutBack));
            sequence.OnKill(() =>
            {
                if (this != null)
                    transform.localPosition = restingLocalPosition;
            });
            return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }
        #endregion

        #region Private Methods
        private void SpawnTileFeatureView(TileFeatureState tileFeatureState)
        {
            if (!tileFeaturePrefabsByType.TryGetValue(tileFeatureState.FeatureType, out TileFeatureView tileFeaturePrefab))
                throw new InvalidOperationException($"No prefab is registered for feature {tileFeatureState.FeatureType}.");

            tileFeatureViewInstance = Instantiate(tileFeaturePrefab, transform, false);
            tileFeatureViewInstance.InitializeFeatureView(tileSize);
            tileFeatureViewInstance.DisplayFeatureState(tileFeatureState);
        }
        #endregion
    }
}
