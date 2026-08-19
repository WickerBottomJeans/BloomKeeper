using UnityEngine;

namespace DefaultNamespace.UI
{
    public readonly struct ScrollPoolItemGeometry
    {
        public Vector2 Position { get; }
        public float HalfExtent { get; }

        public ScrollPoolItemGeometry(Vector2 position, float halfExtent)
        {
            Position = position;
            HalfExtent = halfExtent;
        }
    }

    public interface IScrollPoolGeometrySource
    {
        int Count { get; }
        ScrollPoolItemGeometry GetGeometry(int index);
    }

    public static class ScrollPoolViewportBounds
    {
        public static Rect GetContentLocalRect(RectTransform content, RectTransform viewport, Vector3[] viewportWorldCorners)
        {
            viewport.GetWorldCorners(viewportWorldCorners);

            Vector3 firstCorner = content.InverseTransformPoint(viewportWorldCorners[0]);
            float minX = firstCorner.x;
            float maxX = firstCorner.x;
            float minY = firstCorner.y;
            float maxY = firstCorner.y;

            for (int index = 1; index < viewportWorldCorners.Length; index++)
            {
                Vector3 corner = content.InverseTransformPoint(viewportWorldCorners[index]);
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}
