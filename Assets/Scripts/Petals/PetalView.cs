using DefaultNamespace;
using DefaultNamespace.Utility;
using Petals;
using UnityEngine;

public class PetalView : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [SerializeField] [Range(0f, 1f)] private float paddingXRatio = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float paddingYRatio = 0.2f;
    public Vector3 TargetScale { get; private set; }

    public void Init(Petal petal, float cellSize, PetalSpriteConfig config)
    {
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
        transform.localScale = TargetScale;
    }
}