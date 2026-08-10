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

    public sealed class GardenersGloveRepresentationData : BoosterRepresentationData
    {
        public Vector2Int OriginPosition { get; }
        public Vector2Int TargetPosition { get; }

        public GardenersGloveRepresentationData(Vector2Int originPosition, Vector2Int targetPosition)
        {
            OriginPosition = originPosition;
            TargetPosition = targetPosition;
        }
    }
}
