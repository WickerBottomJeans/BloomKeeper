using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>The swap that directly started this resolution, before any cascades.</summary>
    public readonly struct SwapContext
    {
        public Vector2Int Origin { get; }
        public Vector2Int Destination { get; }

        public SwapContext(Vector2Int origin, Vector2Int destination)
        {
            Origin = origin;
            Destination = destination;
        }
    }
}
