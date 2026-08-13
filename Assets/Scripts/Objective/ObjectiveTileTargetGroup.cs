using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class ObjectiveTileTargetGroup
    {
        public ObjectiveType ObjectiveType { get; }
        public IReadOnlyList<Vector2Int> Positions { get; }

        public ObjectiveTileTargetGroup(ObjectiveType objectiveType, IReadOnlyList<Vector2Int> positions)
        {
            ObjectiveType = objectiveType;
            Positions = positions;
        }
    }
}
