using DefaultNamespace;
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PetalView : PieceView
{
    [SerializeField] private SpriteRenderer bubbleOverlay;
    [SerializeField] private Material aboutToExecuteMaterial;
    [SerializeField] private float bubbleBounceDuration = 0.3f;
    [SerializeField] private float bubbleBounceOvershoot = 2f;
    [SerializeField] private Vector2 petalChangeSquashScale = new Vector2(1.18f, 0.82f);
    [SerializeField] private Vector2 petalChangeStretchScale = new Vector2(0.9f, 1.1f);
    [SerializeField] [Range(0f, 1f)] private float petalChangeSquashDurationRatio = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float petalChangeStretchDurationRatio = 0.35f;
    private Vector3 defaultBubbleScale;
    private Color defaultBubbleColor;
    private int defaultBubbleSortingLayerId;
    private int defaultBubbleSortingOrder;

    protected override void Awake()
    {
        base.Awake();
        defaultBubbleScale = bubbleOverlay.transform.localScale;
        defaultBubbleColor = bubbleOverlay.color;
        defaultBubbleSortingLayerId = bubbleOverlay.sortingLayerID;
        defaultBubbleSortingOrder = bubbleOverlay.sortingOrder;
    }

    public override void Refresh(IBoardPiece piece, float tileSize)
    {
        if (piece is not Petal petal) throw new ArgumentException("A petal view requires a petal.", nameof(piece));
        Sprite sprite = SpriteCatalogProvider.Instance.GetPetalSprite(petal.PetalType, petal.Skill);
        RefreshSprite(sprite, tileSize);
        ConfigureSkillDecorations(petal.Skill);
    }

    public override void ResetForPool()
    {
        base.ResetForPool();
        SetBubbleVisible(false);
    }

    protected override void OnSelectionChanged(bool selected)
    {
        bubbleOverlay.sortingLayerID = selected ? spriteRenderer.sortingLayerID : defaultBubbleSortingLayerId;
        bubbleOverlay.sortingOrder = defaultBubbleSortingOrder + (selected ? boosterSelectionSortingOrderOffset : 0);
    }

    public UniTask PlayPrismaticBloomPrepareSpin(float duration, float maximumSpinSpeed)
    {
        KillVisualAnimation();
        float rotation = maximumSpinSpeed * duration * 0.5f;
        return VisualTransform.DORotate(new Vector3(0f, 0f, rotation), duration, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public UniTask PlayPrismaticBloomFireSpin(float duration, float maximumSpinSpeed)
    {
        KillVisualAnimation();
        float rotation = maximumSpinSpeed * duration;
        return VisualTransform.DORotate(new Vector3(0f, 0f, rotation), duration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public UniTask PlayBubbleInflate(float scaleMultiplier, float duration)
    {
        SetBubbleVisible(true);
        Transform bubbleTransform = bubbleOverlay.transform;
        bubbleTransform.DOKill();
        return bubbleTransform.DOScale(defaultBubbleScale * scaleMultiplier, duration).SetEase(Ease.InQuad).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public UniTask PlayAboutToExecute()
    {
        KillVisualAnimation();
        UseAboutToExecuteMaterial();
        VisualTransform.localScale = TargetScale;

        Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(VisualTransform.DOScale(TargetScale * 1.08f, 0.18f).SetEase(Ease.InOutSine));
        seq.Append(VisualTransform.DOScale(TargetScale * 0.94f, 0.18f).SetEase(Ease.InOutSine));
        seq.Append(VisualTransform.DOScale(TargetScale, 0.18f).SetEase(Ease.InOutSine));
        seq.SetTarget(VisualTransform);
        seq.SetLoops(-1, LoopType.Restart);

        return UniTask.CompletedTask;
    }

    public async UniTask PlayFly(Vector2 targetWorldPosition, float tileSize, float duration, float curveAmplitudeInTiles)
    {
        KillActiveAnimation();
        Vector3 startControlPoint = transform.position + Vector3.up * tileSize * curveAmplitudeInTiles;
        Vector3 endControlPoint = (Vector3)targetWorldPosition + Vector3.up * tileSize * curveAmplitudeInTiles;
        Vector3[] flightPath = { targetWorldPosition, startControlPoint, endControlPoint };
        Vector2 previousPosition = transform.position;

        Tween scaleTween = transform.DOScale(defaultRootScale, duration).SetEase(Ease.InQuad).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        Tween flapTween = VisualTransform.DOScaleX(0f, 0.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        int previousSortingOrder = spriteRenderer.sortingOrder;
        spriteRenderer.sortingOrder = previousSortingOrder + FlyingSortingOrderOffset;
        SyncShadowSorting();

        try
        {
            await transform.DOPath(flightPath, duration, PathType.CubicBezier, PathMode.Sidescroller2D)
                .OnUpdate(() =>
                {
                    Vector2 currentPosition = transform.position;
                    Vector2 flightDirection = currentPosition - previousPosition;
                    if (flightDirection.sqrMagnitude > 0f)
                    {
                        float angle = Vector2.SignedAngle(Vector2.up, flightDirection);
                        VisualTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    }
                    previousPosition = currentPosition;
                })
                .SetEase(Ease.InQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }
        finally
        {
            scaleTween.Kill();
            flapTween.Kill();
            if (this != null)
            {
                transform.localScale = defaultRootScale;
                VisualTransform.localScale = TargetScale;
                spriteRenderer.sortingOrder = previousSortingOrder;
                SyncShadowSorting();
            }
        }
    }

    public async UniTask PlayPetalChange(Petal petal, float tileSize, float duration)
    {
        if (duration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(duration), "Petal change animation requires a positive duration.");

        float settleDurationRatio = 1f - petalChangeSquashDurationRatio - petalChangeStretchDurationRatio;
        if (petalChangeSquashDurationRatio <= 0f || petalChangeStretchDurationRatio <= 0f || settleDurationRatio <= 0f)
            throw new InvalidOperationException("Petal change animation duration ratios must each be positive and total less than one.");

        KillVisualAnimation();
        Vector3 squashScale = new Vector3(petalChangeSquashScale.x, petalChangeSquashScale.y, 1f);
        Vector3 stretchScale = new Vector3(petalChangeStretchScale.x, petalChangeStretchScale.y, 1f);

        Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        _ = sequence.Append(stretchAxis.DOScale(squashScale, duration * petalChangeSquashDurationRatio).SetEase(Ease.InQuad));
        _ = sequence.AppendCallback(() =>
        {
            Refresh(petal, tileSize);
            stretchAxis.localScale = squashScale;
        });
        _ = sequence.Append(stretchAxis.DOScale(stretchScale, duration * petalChangeStretchDurationRatio).SetEase(Ease.OutQuad));
        _ = sequence.Append(stretchAxis.DOScale(Vector3.one, duration * settleDurationRatio).SetEase(Ease.OutBack));
        _ = sequence.OnKill(() =>
        {
            if (this != null)
                ResetDirectionalJelly();
        });

        await sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public void SetBubbleVisible(bool isVisible)
    {
        Transform bubbleTransform = bubbleOverlay.transform;

        if (isVisible && bubbleOverlay.gameObject.activeSelf) return;

        bubbleTransform.DOKill();

        if (!isVisible)
        {
            bubbleTransform.localScale = defaultBubbleScale;
            bubbleOverlay.color = defaultBubbleColor;
            bubbleOverlay.gameObject.SetActive(false);
            return;
        }

        bubbleOverlay.gameObject.SetActive(true);
        bubbleOverlay.color = defaultBubbleColor;
        bubbleTransform.localScale = Vector3.zero;
        bubbleTransform.DOScale(defaultBubbleScale, bubbleBounceDuration).SetEase(Ease.OutBack, bubbleBounceOvershoot).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void ConfigureSkillDecorations(SpecialSkillType skillType)
    {
        SetBubbleVisible(skillType == SpecialSkillType.Bubble);
    }

    private void UseAboutToExecuteMaterial()
    {
        if (aboutToExecuteMaterial == null) return;
        CacheDefaultMaterial();
        spriteRenderer.sharedMaterial = aboutToExecuteMaterial;
    }
}
