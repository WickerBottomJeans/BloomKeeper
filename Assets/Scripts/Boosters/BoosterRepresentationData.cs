using UnityEngine;

namespace Boosters
{
    public abstract class BoosterRepresentationData
    {
    }

    public class BloomWandRepresentationData : BoosterRepresentationData
    {
        public Vector2Int TargetPosition { get; }

        public BloomWandRepresentationData(Vector2Int targetPosition)
        {
            TargetPosition = targetPosition;
        }
    }

    public class GardenersGloveRepresentationData : BoosterRepresentationData
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
