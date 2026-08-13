using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public abstract class SkillRepresentationData
    {
        public IReadOnlyList<Vector2Int> ConsumedInputPositions { get; }

        protected SkillRepresentationData(IReadOnlyList<Vector2Int> consumedInputPositions)
        {
            ConsumedInputPositions = consumedInputPositions;
        }
    }

    public class BubbleRepresentationData : SkillRepresentationData
    {
        public Vector2Int Center { get; }
        public IReadOnlyList<Vector2Int> AffectedPositions { get; }

        public BubbleRepresentationData(Vector2Int center, IReadOnlyList<Vector2Int> affectedPositions, IReadOnlyList<Vector2Int> consumedInputPositions) : base(consumedInputPositions)
        {
            Center = center;
            AffectedPositions = affectedPositions;
        }
    }

    public class StripedRepresentationData : SkillRepresentationData
    {
        /// <summary>
        /// Coord of the Petal skill which is being executed in the Grid
        /// </summary>
        public Vector2Int Source { get; }
        public SpecialSkillType Direction { get; }
        public IReadOnlyList<Vector2Int> AffectedPositions { get; }

        public StripedRepresentationData(Vector2Int source, SpecialSkillType direction, IReadOnlyList<Vector2Int> affectedPositions, IReadOnlyList<Vector2Int> consumedInputPositions) : base(consumedInputPositions)
        {
            Source = source;
            Direction = direction;
            AffectedPositions = affectedPositions;
        }
    }

    public class StripeStripeFusionRepresentationData : SkillRepresentationData
    {
        public Vector2Int Anchor { get; }
        public IReadOnlyList<Vector2Int> AffectedPositions { get; }

        public StripeStripeFusionRepresentationData(Vector2Int anchor, IReadOnlyList<Vector2Int> affectedPositions, IReadOnlyList<Vector2Int> consumedInputPositions) : base(consumedInputPositions)
        {
            Anchor = anchor;
            AffectedPositions = affectedPositions;
        }
    }

    public class ButterflyRepresentationData : SkillRepresentationData
    {
        public Vector2Int Source { get; }
        public Vector2Int? Target { get; }
        public PetalType SourcePetalType { get; }

        public ButterflyRepresentationData(Vector2Int source, Vector2Int? target, PetalType sourcePetalType, IReadOnlyList<Vector2Int> consumedInputPositions) : base(consumedInputPositions)
        {
            Source = source;
            Target = target;
            SourcePetalType = sourcePetalType;
        }
    }

    public class PrismaticBloomRepresentationData : SkillRepresentationData
    {
        public Vector2Int Source { get; }
        public SpecialSkillType ReplacementSkill { get; }
        public IReadOnlyList<PetalChange> Changes { get; }

        public PrismaticBloomRepresentationData(Vector2Int source, SpecialSkillType replacementSkill, IReadOnlyList<PetalChange> changes, IReadOnlyList<Vector2Int> consumedInputPositions) : base(consumedInputPositions)
        {
            Source = source;
            ReplacementSkill = replacementSkill;
            Changes = changes;
        }
    }
}
