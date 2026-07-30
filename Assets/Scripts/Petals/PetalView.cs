using DefaultNamespace;
using DefaultNamespace.Utility;
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Petals;
using UnityEngine;

public class PetalView : MonoBehaviour
{
    private const int FlyingSortingOrderOffset = 1;
    private const int ShadowSortingOrderOffset = -1;

    public SpriteRenderer spriteRenderer;

    [SerializeField] private Transform stretchAxis;
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] private SpriteRenderer bubbleOverlay;
    [SerializeField] private Material aboutToExecuteMaterial;
    [SerializeField] private float bubbleBounceDuration = 0.3f;
    [SerializeField] private float bubbleBounceOvershoot = 2f;
    [SerializeField] [Range(0f, 1f)] private float paddingXRatio = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float paddingYRatio = 0.2f;
    [SerializeField] [Range(0f, 0.25f)] private float directionalJellyStrength = 0.025f;
    [SerializeField] [Range(0f, 0.95f)] private float maxDirectionalJellyStretch = 0.16f;
    [SerializeField] [Range(0f, 1f)] private float directionalJellySquashDurationRatio = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float directionalJellySettleDurationRatio = 0.3f;
    [SerializeField] private Vector2 petalChangeSquashScale = new Vector2(1.18f, 0.82f);
    [SerializeField] private Vector2 petalChangeStretchScale = new Vector2(0.9f, 1.1f);
    [SerializeField] [Range(0f, 1f)] private float petalChangeSquashDurationRatio = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float petalChangeStretchDurationRatio = 0.35f;
    private Vector3 TargetScale { get; set; }
    public Transform VisualTransform => spriteRenderer.transform;
    private Material defaultMaterial;
    private Color defaultColor;
    private Color defaultShadowColor;
    private Vector3 defaultRootScale;
    private Vector3 defaultBubbleScale;
    private Color defaultBubbleColor;
    private int defaultSortingOrder;

    private void Awake()
    {
        defaultColor = spriteRenderer.color;
        defaultShadowColor = shadowRenderer.color;
        defaultRootScale = transform.localScale;
        defaultBubbleScale = bubbleOverlay.transform.localScale;
        defaultBubbleColor = bubbleOverlay.color;
        defaultSortingOrder = spriteRenderer.sortingOrder;
        SyncShadowSorting();
        CacheDefaultMaterial();
    }

    public void Refresh(Petal petal, float tileSize)
    {
        CacheDefaultMaterial();
        RestoreDefaultMaterial();
        string spriteKey = SpriteKeyHelper.GetPetalSpriteKey(petal.PetalType, petal.Skill);
        var sprite = SpriteLoader.Instance.GetSprite(spriteKey);
        if (sprite == null)
        {
            Debug.LogError($"Sprite {spriteKey} not found");
            return;
        }
        spriteRenderer.sprite = sprite;
        shadowRenderer.sprite = sprite;
        SyncShadowSorting();

        Vector2 spriteWorldSize = sprite.bounds.size;

        float targetWidth = tileSize * (1f - paddingXRatio);
        float targetHeight = tileSize * (1f - paddingYRatio);

        float scale = Mathf.Min(
            targetWidth / spriteWorldSize.x,
            targetHeight / spriteWorldSize.y
        );
        TargetScale = Vector3.one * scale;
        transform.localScale = defaultRootScale;
        ResetDirectionalJelly();
        VisualTransform.localPosition = Vector3.zero;
        VisualTransform.localScale = TargetScale;
        ConfigureSkillDecorations(petal.Skill);
    }

    public UniTask PlaySwap(Vector2 targetPos, float tileSize)
    {
        return PlayJellyishMove(targetPos, tileSize, 0.2f, Ease.OutQuad);
    }

    public UniTask PlayDisappear(float duration, Ease ease = Ease.InBack, float delay = 0f)
    {
        KillVisualTweens();
        RestoreDefaultMaterial();
        Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.AppendInterval(delay);
        sequence.Append(VisualTransform.DOScale(Vector3.zero, duration).SetEase(ease));
        return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public UniTask PlayScale(float scaleMultiplier, float duration, Ease ease = Ease.OutBack)
    {
        KillVisualAnimation();
        return VisualTransform.DOScale(TargetScale * scaleMultiplier, duration)
            .SetEase(ease)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public UniTask PlayRootScale(float scaleMultiplier, float duration)
    {
        KillRootAnimation();
        return transform.DOScale(defaultRootScale * scaleMultiplier, duration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
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

    public UniTask PlaySpawn()
    {
        KillVisualAnimation();
        VisualTransform.localScale = Vector3.zero;
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0f);
        shadowRenderer.color = new Color(shadowRenderer.color.r, shadowRenderer.color.g, shadowRenderer.color.b, 0f);

        Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(VisualTransform.DOScale(TargetScale, 0.2f).SetEase(Ease.OutBack));
        seq.Join(spriteRenderer.DOFade(1f, 0.2f));
        seq.Join(shadowRenderer.DOFade(defaultShadowColor.a, 0.2f));
        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public UniTask PlayDrop(Vector2 targetPos, float tileSize)
    {
        return PlayJellyishMove(targetPos, tileSize, 0.25f, Ease.InQuad);
    }

    public UniTask PlayGather(Vector2 targetPos, float duration = 0.3f)
    {
        KillActiveAnimation();
        return transform.DOMove(targetPos, duration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
    }

    public UniTask PlayJellyishMove(Vector2 targetPos, float tileSize, float duration, Ease moveEase)
    {
        if (tileSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(tileSize), "Directional jelly movement requires a positive tile size.");
        if (duration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(duration), "Directional jelly movement requires a positive duration.");

        KillRootAnimation();
        stretchAxis.DOKill();
        ResetDirectionalJelly();

        Vector2 startPos = transform.position;
        Vector2 displacement = targetPos - startPos;

        if (displacement.sqrMagnitude <= Mathf.Epsilon)
            return transform.DOMove(targetPos, duration).SetEase(moveEase).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());

        PrepareDirectionalJelly(displacement, tileSize, duration, out Vector3 stretchScale, out Vector3 squashScale);

        float squashDuration = duration * directionalJellySquashDurationRatio;
        float settleDuration = duration * directionalJellySettleDurationRatio;

        Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.SetTarget(transform);
        seq.Join(transform.DOMove(targetPos, duration).SetEase(moveEase));
        seq.Join(stretchAxis.DOScale(stretchScale, duration).SetEase(Ease.OutQuad));
        seq.Append(stretchAxis.DOScale(squashScale, squashDuration).SetEase(Ease.InOutSine));
        seq.Append(stretchAxis.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutBack));
        seq.OnKill(() =>
        {
            if (this != null)
                ResetDirectionalJelly();
        });
        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
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

    private void KillActiveAnimation()
    {
        KillRootAnimation();
        KillVisualAnimation();
    }

    public void ResetForPool()
    {
        KillActiveAnimation();
        transform.localScale = defaultRootScale;
        spriteRenderer.color = defaultColor;
        shadowRenderer.color = defaultShadowColor;
        spriteRenderer.sortingOrder = defaultSortingOrder;
        SyncShadowSorting();
        SetBubbleVisible(false);
        RestoreDefaultMaterial();
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

    private void KillRootAnimation()
    {
        transform.DOKill();
    }

    private void KillVisualAnimation()
    {
        KillVisualTweens();
        ResetDirectionalJelly();
        RestoreDefaultMaterial();
    }

    private void KillVisualTweens()
    {
        stretchAxis.DOKill();
        VisualTransform.DOKill();
        spriteRenderer.DOKill();
        shadowRenderer.DOKill();
    }

    private void ResetDirectionalJelly()
    {
        stretchAxis.localRotation = Quaternion.identity;
        stretchAxis.localScale = Vector3.one;
        VisualTransform.localRotation = Quaternion.identity;
    }

    private void UseAboutToExecuteMaterial()
    {
        if (aboutToExecuteMaterial == null) return;
        CacheDefaultMaterial();
        spriteRenderer.sharedMaterial = aboutToExecuteMaterial;
    }

    private void RestoreDefaultMaterial()
    {
        if (defaultMaterial == null) return;
        spriteRenderer.sharedMaterial = defaultMaterial;
    }

    private void CacheDefaultMaterial()
    {
        if (defaultMaterial != null) return;
        defaultMaterial = spriteRenderer.sharedMaterial;
    }

    private void SyncShadowSorting()
    {
        shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder + ShadowSortingOrderOffset;
    }

    private void PrepareDirectionalJelly(Vector2 displacement, float tileSize, float duration, out Vector3 stretchScale, out Vector3 squashScale)
    {
        float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;
        float speedTilesPerSecond = displacement.magnitude / tileSize / duration;
        float stretchAmount = Mathf.Clamp(speedTilesPerSecond * directionalJellyStrength, 0f, maxDirectionalJellyStretch);

        stretchAxis.localRotation = Quaternion.Euler(0f, 0f, angle);
        VisualTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

        stretchScale = new Vector3(1f + stretchAmount, 1f - stretchAmount, 1f);
        squashScale = new Vector3(1f - stretchAmount, 1f + stretchAmount, 1f);
    }
}
