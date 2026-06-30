using UnityEngine;

namespace DefaultNamespace
{
    public sealed class WorldLevelBackground : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void FitWidthToCamera(Camera camera)
        {
            if (camera == null) return;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;

            float visibleWorldHeight = camera.orthographicSize * 2f;
            float visibleWorldWidth = visibleWorldHeight * camera.aspect;
            Vector2 spriteWorldSize = spriteRenderer.sprite.bounds.size;
            float scale = visibleWorldWidth / spriteWorldSize.x;

            transform.localScale = new Vector3(scale, scale, transform.localScale.z);
            transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, transform.position.z);
        }
    }
}
