using DefaultNamespace;
using DefaultNamespace.Utility;
using DG.Tweening;
using Petals;
using UnityEngine;

public class PetalView : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [SerializeField] private Transform stretchAxis;
    [SerializeField] private Material aboutToExecuteMaterial;
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
    private Material defaultMaterial;

    private void Awake()
    {
        CacheDefaultMaterial();
    }

    public void Init(Petal petal, float cellSize)
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

        float targetWidth = cellSize * (1f - paddingXRatio);
        float targetHeight = cellSize * (1f - paddingYRatio);

        float scale = Mathf.Min(
            targetWidth / spriteWorldSize.x,
            targetHeight / spriteWorldSize.y
        );
        TargetScale = Vector3.one * scale;
        transform.localScale = Vector3.one;
        ResetDirectionalJelly();
        VisualTransform.localPosition = Vector3.zero;
        VisualTransform.localScale = TargetScale;
    }

    public void KillActiveAnimation()
    {
        KillRootAnimation();
        KillVisualAnimation();
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
