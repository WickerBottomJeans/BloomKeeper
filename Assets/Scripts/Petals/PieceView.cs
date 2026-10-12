using DefaultNamespace;
using DefaultNamespace.Utility;
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>Common sizing, movement, selection and pool reset. Concrete views own piece-specific visuals.</summary>
public abstract class PieceView : MonoBehaviour
{
    protected const int FlyingSortingOrderOffset = 1;
    private const int ShadowSortingOrderOffset = -1;

    public SpriteRenderer spriteRenderer;

    [SerializeField] protected Transform stretchAxis;
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] [Range(0f, 1f)] private float paddingXRatio = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float paddingYRatio = 0.2f;
    [SerializeField] [Range(0f, 0.25f)] private float directionalJellyStrength = 0.025f;
    [SerializeField] [Range(0f, 0.95f)] private float maxDirectionalJellyStretch = 0.16f;
    [SerializeField] [Range(0f, 1f)] private float directionalJellySquashDurationRatio = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float directionalJellySettleDurationRatio = 0.3f;
    [SerializeField] private float boosterSelectionScaleMultiplier = 1.2f;
    [SerializeField] private float boosterSelectionPulseHalfDuration = 0.25f;
    [SerializeField] private float boosterSelectionPeakScaleMultiplier = 1.6f;
    [SerializeField] protected int boosterSelectionSortingOrderOffset = 10;
    protected Vector3 TargetScale { get; set; }
    public Transform VisualTransform => spriteRenderer.transform;
    private Material defaultMaterial;
    private Color defaultColor;
    private Color defaultShadowColor;
    protected Vector3 defaultRootScale;
    private int defaultSortingOrder;
    private bool isBoosterSelected;

    protected virtual void Awake()
    {
        defaultColor = spriteRenderer.color;
        defaultShadowColor = shadowRenderer.color;
        defaultRootScale = transform.localScale;
        defaultSortingOrder = spriteRenderer.sortingOrder;
        SyncShadowSorting();
        CacheDefaultMaterial();
    }

    public abstract void Refresh(IBoardPiece piece, float tileSize);
    protected virtual void OnSelectionChanged(bool selected) { }

    protected void RefreshSprite(Sprite sprite, float tileSize)
    {
        CacheDefaultMaterial();
        RestoreDefaultMaterial();
        spriteRenderer.sprite = sprite;
        shadowRenderer.sprite = sprite;
        SyncShadowSorting();
        Vector2 spriteWorldSize = sprite.bounds.size;
        float scale = Mathf.Min(tileSize * (1f - paddingXRatio) / spriteWorldSize.x, tileSize * (1f - paddingYRatio) / spriteWorldSize.y);
        TargetScale = Vector3.one * scale;
        transform.localScale = defaultRootScale;
        ResetDirectionalJelly();
        VisualTransform.localPosition = Vector3.zero;
        VisualTransform.localScale = TargetScale;
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

    public void ShowBoosterSelection()
    {
        if (isBoosterSelected) throw new InvalidOperationException("Piece booster selection is already visible.");

        isBoosterSelected = true;
        KillVisualAnimation();
        VisualTransform.localScale = TargetScale;
        spriteRenderer.sortingOrder = defaultSortingOrder + boosterSelectionSortingOrderOffset;
        SyncShadowSorting();
        OnSelectionChanged(true);
        VisualTransform.localScale = TargetScale * boosterSelectionScaleMultiplier;
        VisualTransform.DOScale(TargetScale * boosterSelectionPeakScaleMultiplier, boosterSelectionPulseHalfDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void HideBoosterSelection()
    {
        if (!isBoosterSelected) throw new InvalidOperationException("Piece booster selection is not visible.");

        RestoreBoosterSelection();
    }

    public UniTask PlayRootScale(float scaleMultiplier, float duration)
    {
        KillRootAnimation();
        return transform.DOScale(defaultRootScale * scaleMultiplier, duration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
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

    protected void KillActiveAnimation()
    {
        KillRootAnimation();
        KillVisualAnimation();
    }

    public virtual void ResetForPool()
    {
        KillActiveAnimation();
        RestoreBoosterSelection();
        transform.localScale = defaultRootScale;
        transform.localRotation = Quaternion.identity;
        spriteRenderer.color = defaultColor;
        shadowRenderer.color = defaultShadowColor;
        spriteRenderer.sortingOrder = defaultSortingOrder;
        SyncShadowSorting();
        RestoreDefaultMaterial();
    }

    protected void KillRootAnimation()
    {
        transform.DOKill();
    }

    protected void KillVisualAnimation()
    {
        KillVisualTweens();
        ResetDirectionalJelly();
        RestoreDefaultMaterial();
    }

    protected void KillVisualTweens()
    {
        stretchAxis.DOKill();
        VisualTransform.DOKill();
        spriteRenderer.DOKill();
        shadowRenderer.DOKill();
    }

    protected void ResetDirectionalJelly()
    {
        stretchAxis.localRotation = Quaternion.identity;
        stretchAxis.localScale = Vector3.one;
        VisualTransform.localRotation = Quaternion.identity;
    }

    protected void RestoreDefaultMaterial()
    {
        if (defaultMaterial == null) return;
        spriteRenderer.sharedMaterial = defaultMaterial;
    }

    protected void CacheDefaultMaterial()
    {
        if (defaultMaterial != null) return;
        defaultMaterial = spriteRenderer.sharedMaterial;
    }

    protected void SyncShadowSorting()
    {
        shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder + ShadowSortingOrderOffset;
    }

    private void RestoreBoosterSelection()
    {
        isBoosterSelected = false;
        VisualTransform.DOKill();
        VisualTransform.localScale = TargetScale;
        spriteRenderer.sortingOrder = defaultSortingOrder;
        SyncShadowSorting();
        OnSelectionChanged(false);
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
