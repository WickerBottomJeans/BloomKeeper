using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public sealed class ObjectiveBoardCellTargetGroup
    {
        public ObjectiveType ObjectiveType { get; }
        public IReadOnlyList<Vector2Int> Positions { get; }

        public ObjectiveBoardCellTargetGroup(ObjectiveType objectiveType, IReadOnlyList<Vector2Int> positions)
        {
            ObjectiveType = objectiveType;
            Positions = positions;
        }
    }
}
