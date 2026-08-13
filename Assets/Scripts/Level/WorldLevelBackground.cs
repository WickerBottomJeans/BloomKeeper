using UnityEngine;

namespace DefaultNamespace
{
    public class WorldLevelBackground : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void Show(Camera camera)
        {
            gameObject.SetActive(true);
            FitHeightToCamera(camera);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void FitHeightToCamera(Camera camera)
        {
            if (camera == null) return;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;

            float visibleWorldHeight = camera.orthographicSize * 2f;
            Vector2 spriteWorldSize = spriteRenderer.sprite.bounds.size;
            float scale = visibleWorldHeight / spriteWorldSize.y;

            transform.localScale = new Vector3(scale, scale, transform.localScale.z);
            transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, transform.position.z);
        }
    }
}
