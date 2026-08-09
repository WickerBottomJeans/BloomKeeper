using UnityEngine;

namespace Boosters
{
    public abstract class BoosterRepresentationData
    {
    }

    public sealed class BloomWandRepresentationData : BoosterRepresentationData
    {
        public Vector2Int TargetPosition { get; }

        public BloomWandRepresentationData(Vector2Int targetPosition)
        {
            TargetPosition = targetPosition;
        }
    }
}
