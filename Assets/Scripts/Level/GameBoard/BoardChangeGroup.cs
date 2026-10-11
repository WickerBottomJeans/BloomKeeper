using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>Tile changes from one match, skill impact, or clear.</summary>
    public class BoardChangeGroup
    {
        /// <summary>The match shape, if a match caused this group.</summary>
        public MatchShape MatchShape { get; }

        /// <summary>The skill type that caused this group, if any.</summary>
        public SpecialSkillType CausingSkill { get; }

        /// <summary>The individual changes in this group.</summary>
        public IReadOnlyList<TileChange> TileChanges { get; }

        /// <summary>Positions where a cleared skill petal activated.</summary>
        public IReadOnlyList<Vector2Int> TriggeredSkillPositions { get; }

        public BoardChangeGroup(IReadOnlyList<TileChange> tileChanges, IReadOnlyList<Vector2Int> triggeredSkillPositions, MatchShape matchShape = MatchShape.None, SpecialSkillType causingSkill = SpecialSkillType.None)
        {
            MatchShape = matchShape;
            CausingSkill = causingSkill;
            TileChanges = new List<TileChange>(tileChanges).AsReadOnly();
            TriggeredSkillPositions = triggeredSkillPositions;
        }

        public IEnumerable<Vector2Int> GetTriggeredSkillInputPositions()
        {
            return TriggeredSkillPositions;
        }

        public bool IsTriggeredSkillInputPosition(Vector2Int position)
        {
            foreach (Vector2Int triggeredPosition in TriggeredSkillPositions)
                if (triggeredPosition == position) return true;

            return false;
        }
    }
}
