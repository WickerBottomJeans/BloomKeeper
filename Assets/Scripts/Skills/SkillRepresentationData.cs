using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public abstract class SkillRepresentationData
    {
    }

    public sealed class BouquetRepresentationData : SkillRepresentationData
    {
        public Vector2Int Center { get; }
        public IReadOnlyList<Vector2Int> AffectedPositions { get; }

        public BouquetRepresentationData(
            Vector2Int center,
            IReadOnlyList<Vector2Int> affectedPositions)
        {
            Center = center;
            AffectedPositions = affectedPositions;
        }
    }

    public sealed class StripedRepresentationData : SkillRepresentationData
    {
        /// <summary>
        /// Coord of the Petal skill which is being executed in the Grid
        /// </summary>
        public Vector2Int Source { get; }
        public SpecialSkillType Direction { get; }
        public IReadOnlyList<Vector2Int> AffectedPositions { get; }

        public StripedRepresentationData(
            Vector2Int source,
            SpecialSkillType direction,
            IReadOnlyList<Vector2Int> affectedPositions)
        {
            Source = source;
            Direction = direction;
            AffectedPositions = affectedPositions;
        }
    }

    public sealed class ButterflyRepresentationData : SkillRepresentationData
    {
        public Vector2Int Source { get; }
        public Vector2Int? Target { get; }

        public ButterflyRepresentationData(Vector2Int source, Vector2Int? target)
        {
            Source = source;
            Target = target;
        }
    }

    public sealed class StripeSunburstRepresentationData : SkillRepresentationData
    {
        public Vector2Int SourceA { get; }
        public Vector2Int SourceB { get; }
        public Vector2 Origin { get; }
        public IReadOnlyList<PetalChange> Changes { get; }

        public StripeSunburstRepresentationData(
            Vector2Int sourceA,
            Vector2Int sourceB,
            Vector2 origin,
            IReadOnlyList<PetalChange> changes)
        {
            SourceA = sourceA;
            SourceB = sourceB;
            Origin = origin;
            Changes = changes;
        }
    }
}
