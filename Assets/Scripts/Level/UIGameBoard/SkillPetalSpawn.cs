using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class SkillPetalSpawn
    {
        public IReadOnlyList<Vector2Int> ContributorPositions { get; }
        public Vector2Int SpawnPosition { get; }
        public PetalType PetalType { get; }
        public SpecialSkillType SkillType { get; }

        public SkillPetalSpawn(IReadOnlyList<Vector2Int> contributorPositions, Vector2Int spawnPosition, PetalType petalType, SpecialSkillType skillType)
        {
            ContributorPositions = new List<Vector2Int>(contributorPositions);
            SpawnPosition = spawnPosition;
            PetalType = petalType;
            SkillType = skillType;
        }
    }
}
