using System.Collections.Generic;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public abstract class SkillRepresentationData
    {
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
