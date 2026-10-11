using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public class SkillExecutionContext
    {
        public Tile[,] Grid { get; }
        public IReadOnlyList<ObjectiveTileTargetGroup> ObjectiveTargetGroups { get; }
        public System.Random Random { get; }

        public SkillExecutionContext(Tile[,] grid, IReadOnlyList<ObjectiveTileTargetGroup> objectiveTargetGroups, System.Random random = null)
        {
            Grid = grid;
            ObjectiveTargetGroups = objectiveTargetGroups;
            Random = random ?? new System.Random();
        }
    }
}
