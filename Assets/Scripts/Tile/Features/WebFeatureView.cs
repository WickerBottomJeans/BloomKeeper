using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public class WebFeatureView : TileFeatureView
    {
        [SerializeField] private SpriteRenderer webRenderer;
        [SerializeField] private Transform spiderLayoutRoot;
        [SerializeField] private GameObject spiderTemplate;
        [SerializeField] private SpiderLayout[] spiderLayouts;
        [SerializeField] private float spiderMovementDuration = 0.2f;
        [SerializeField] private float removalDuration = 0.15f;

        private readonly List<GameObject> spiderInstances = new List<GameObject>();

        #region Public API
        public override TileFeatureType FeatureType => TileFeatureType.Web;

        public override void InitializeFeatureView(float tileSize)
        {
            if (tileSize <= 0f) throw new ArgumentOutOfRangeException(nameof(tileSize));
            spiderTemplate.SetActive(false);
            Sprite webSprite = webRenderer.sprite;
            Vector3 webScale = webRenderer.transform.localScale;
            float webSize = Mathf.Max(webSprite.bounds.size.x * Mathf.Abs(webScale.x), webSprite.bounds.size.y * Mathf.Abs(webScale.y));
            transform.localScale = Vector3.one * (tileSize / webSize);
        }

        public override void DisplayFeatureState(TileFeatureState tileFeatureState)
        {
            if (tileFeatureState is not WebFeatureState webFeatureState) throw new ArgumentException("A web view requires a web snapshot.", nameof(tileFeatureState));
            ApplySpiderLayout(webFeatureState.RemainingLayers);
        }

        public override UniTask PlayFeatureSpawn() => UniTask.CompletedTask;

        public override UniTask PlayFeatureChange(TileFeatureState tileFeatureState)
        {
            if (tileFeatureState is not WebFeatureState webFeatureState) throw new ArgumentException("A web view requires a web snapshot.", nameof(tileFeatureState));
            return AnimateSpiderLayout(webFeatureState.RemainingLayers);
        }

        public override UniTask PlayFeatureRemoval()
        {
            foreach (GameObject spiderInstance in spiderInstances) spiderInstance.SetActive(false);

            return webRenderer.DOFade(0f, removalDuration).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }
        #endregion

        #region Private Methods
        private void ApplySpiderLayout(int remainingLayers, Sequence spiderMovementSequence = null)
        {
            Transform[] spiderPositions = spiderLayouts[remainingLayers - 1].SpiderPositions;
            while (spiderInstances.Count < remainingLayers)
                spiderInstances.Add(Instantiate(spiderTemplate, spiderLayoutRoot, false));

            for (int spiderIndex = 0; spiderIndex < spiderInstances.Count; spiderIndex++)
            {
                GameObject spiderInstance = spiderInstances[spiderIndex];
                if (spiderIndex >= remainingLayers)
                {
                    spiderInstance.SetActive(false);
                    continue;
                }

                Transform spiderTransform = spiderInstance.transform;
                Vector3 spiderTargetPosition = spiderTransform.parent.InverseTransformPoint(spiderPositions[spiderIndex].position);
                if (spiderMovementSequence != null && spiderInstance.activeSelf)
                    spiderMovementSequence.Insert(0f, spiderTransform.DOLocalMove(spiderTargetPosition, spiderMovementDuration).SetEase(Ease.InOutSine));
                else
                    spiderTransform.localPosition = spiderTargetPosition;

                spiderInstance.SetActive(true);
            }
        }

        private UniTask AnimateSpiderLayout(int remainingLayers)
        {
            Sequence spiderMovementSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            spiderMovementSequence.AppendInterval(spiderMovementDuration);
            ApplySpiderLayout(remainingLayers, spiderMovementSequence);
            return spiderMovementSequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }
        #endregion

        [Serializable]
        private class SpiderLayout
        {
            [SerializeField] private Transform[] spiderPositions;

            public Transform[] SpiderPositions => spiderPositions;
        }
    }
}
