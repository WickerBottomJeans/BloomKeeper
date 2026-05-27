using DefaultNamespace;
using UnityEngine;

public class PetalView : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [SerializeField] [Range(0f, 1f)] private float paddingXRatio = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float paddingYRatio = 0.2f;
    public Vector3 TargetScale { get; private set; }

    public void Init(Petal petal, float cellSize, PetalSpriteConfig config)
    {
        spriteRenderer.sprite = config.GetSprite(
            petal.PetalType,
            petal.Skill?.SkillType ?? SpecialSkillType.None
        );

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