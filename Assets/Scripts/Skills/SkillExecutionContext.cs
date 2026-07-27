using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public sealed class SkillExecutionContext
    {
        public BoardCell[,] Grid { get; }
        public IReadOnlyList<ObjectiveBoardCellTargetGroup> ObjectiveTargetGroups { get; }
        public Dictionary<Vector2Int, int> AssignedButterflyCounts { get; } = new Dictionary<Vector2Int, int>();

        public SkillExecutionContext(BoardCell[,] grid, IReadOnlyList<ObjectiveBoardCellTargetGroup> objectiveTargetGroups)
        {
            Grid = grid;
            ObjectiveTargetGroups = objectiveTargetGroups;
        }
    }
}
