using DefaultNamespace;
using DefaultNamespace.Utility;
using DG.Tweening;
using Petals;
using UnityEngine;

public class PetalView : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [SerializeField] private Transform stretchAxis;
    [SerializeField] private SpriteRenderer bubbleOverlay;
    [SerializeField] private Material aboutToExecuteMaterial;
    [SerializeField] private float bubbleBounceDuration = 0.3f;
    [SerializeField] private float bubbleBounceOvershoot = 2f;
    [SerializeField] private float bubblePopScaleMultiplier = 1.7f;
    [SerializeField] [Range(0f, 1f)] private float paddingXRatio = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float paddingYRatio = 0.2f;
    [SerializeField] [Range(0f, 0.25f)] private float directionalJellyStrength = 0.025f;
    [SerializeField] [Range(0f, 0.95f)] private float maxDirectionalJellyStretch = 0.16f;
    [SerializeField] [Range(0f, 1f)] private float directionalJellySquashDurationRatio = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float directionalJellySettleDurationRatio = 0.3f;
    public Vector3 TargetScale { get; private set; }
    public Transform StretchAxis => stretchAxis;
    public Transform VisualTransform => spriteRenderer.transform;
    public float DirectionalJellyStrength => directionalJellyStrength;
    public float MaxDirectionalJellyStretch => maxDirectionalJellyStretch;
    public float DirectionalJellySquashDurationRatio => directionalJellySquashDurationRatio;
    public float DirectionalJellySettleDurationRatio => directionalJellySettleDurationRatio;
    public Vector3 DefaultRootScale => defaultRootScale;
    public Transform BubbleTransform => bubbleOverlay.transform;
    public SpriteRenderer BubbleRenderer => bubbleOverlay;
    public Vector3 DefaultBubbleScale => defaultBubbleScale;
    public float BubblePopScaleMultiplier => bubblePopScaleMultiplier;
    private Material defaultMaterial;
    private Color defaultColor;
    private Vector3 defaultRootScale;
    private Vector3 defaultBubbleScale;
    private Color defaultBubbleColor;

    private void Awake()
    {
        defaultColor = spriteRenderer.color;
        defaultRootScale = transform.localScale;
        defaultBubbleScale = bubbleOverlay.transform.localScale;
        defaultBubbleColor = bubbleOverlay.color;
        CacheDefaultMaterial();
    }

    public void Init(Petal petal, float tileSize)
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
        spriteRenderer.sprite = SpriteLoader.Instance.GetSprite(spriteKey);

        Vector2 spriteWorldSize = spriteRenderer.sprite.bounds.size;

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

    public void KillActiveAnimation()
    {
        KillRootAnimation();
        KillVisualAnimation();
    }

    public void ResetForPool()
    {
        KillActiveAnimation();
        transform.localScale = defaultRootScale;
        spriteRenderer.color = defaultColor;
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

    public void KillRootAnimation()
    {
        transform.DOKill();
    }

    public void KillVisualAnimation()
    {
        stretchAxis.DOKill();
        VisualTransform.DOKill();
        spriteRenderer.DOKill();
        ResetDirectionalJelly();
        RestoreDefaultMaterial();
    }

    public void ResetDirectionalJelly()
    {
        stretchAxis.localRotation = Quaternion.identity;
        stretchAxis.localScale = Vector3.one;
        VisualTransform.localRotation = Quaternion.identity;
    }

    public void UseAboutToExecuteMaterial()
    {
        if (aboutToExecuteMaterial == null) return;
        CacheDefaultMaterial();
        spriteRenderer.sharedMaterial = aboutToExecuteMaterial;
    }

    public void RestoreDefaultMaterial()
    {
        if (defaultMaterial == null) return;
        spriteRenderer.sharedMaterial = defaultMaterial;
    }

    private void CacheDefaultMaterial()
    {
        if (defaultMaterial != null) return;
        defaultMaterial = spriteRenderer.sharedMaterial;
    }
}
