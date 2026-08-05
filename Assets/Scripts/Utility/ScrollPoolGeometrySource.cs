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
}
