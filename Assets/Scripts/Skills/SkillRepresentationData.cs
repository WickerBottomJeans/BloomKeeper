using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public abstract class SkillRepresentationData
    {
    }

    public sealed class BubbleRepresentationData : SkillRepresentationData
    {
        public Vector2Int Center { get; }
        public IReadOnlyList<Vector2Int> AffectedPositions { get; }

        public BubbleRepresentationData(
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
        public PetalType SourcePetalType { get; }

        public ButterflyRepresentationData(Vector2Int source, Vector2Int? target, PetalType sourcePetalType)
        {
            Source = source;
            Target = target;
            SourcePetalType = sourcePetalType;
        }
    }

    public sealed class SunburstRepresentationData : SkillRepresentationData
    {
        public Vector2Int ParticipantA { get; }
        public Vector2Int? ParticipantB { get; }
        public SpecialSkillType ReplacementSkill { get; }
        public IReadOnlyList<PetalChange> Changes { get; }

        public SunburstRepresentationData(Vector2Int participantA, Vector2Int? participantB, SpecialSkillType replacementSkill, IReadOnlyList<PetalChange> changes)
        {
            ParticipantA = participantA;
            ParticipantB = participantB;
            ReplacementSkill = replacementSkill;
            Changes = changes;
        }
    }
}
