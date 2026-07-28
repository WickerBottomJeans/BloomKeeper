using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public sealed class SkillExecutionContext
    {
        public Tile[,] Grid { get; }
        public IReadOnlyList<ObjectiveTileTargetGroup> ObjectiveTargetGroups { get; }
        public Dictionary<Vector2Int, int> AssignedButterflyCounts { get; } = new Dictionary<Vector2Int, int>();

        public SkillExecutionContext(Tile[,] grid, IReadOnlyList<ObjectiveTileTargetGroup> objectiveTargetGroups)
        {
            Grid = grid;
            ObjectiveTargetGroups = objectiveTargetGroups;
        }
    }
}
