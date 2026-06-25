using DefaultNamespace;
using DefaultNamespace.Utility;
using DG.Tweening;
using Petals;
using UnityEngine;

public class PetalView : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [SerializeField] private Material aboutToExecuteMaterial;
    [SerializeField] [Range(0f, 1f)] private float paddingXRatio = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float paddingYRatio = 0.2f;
    public Vector3 TargetScale { get; private set; }
    public Transform VisualTransform => spriteRenderer.transform;
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
        VisualTransform.localPosition = Vector3.zero;
        VisualTransform.localRotation = Quaternion.identity;
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
        VisualTransform.DOKill();
        spriteRenderer.DOKill();
        RestoreDefaultMaterial();
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
