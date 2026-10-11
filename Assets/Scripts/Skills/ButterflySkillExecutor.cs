using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public static class ButterflySkillExecutor
    {
        public static SkillPlan Execute(SkillExecutionContext context, SkillActivation activation)
        {
            SkillParticipant consumedInput = activation.GetOnlyConsumedInput();
            Vector2Int source = consumedInput.Position;
            Petal causer = new Petal(consumedInput.Petal);
            IReadOnlyList<Vector2Int> consumedInputPositions = activation.GetConsumedInputPositions();
            Vector2Int? target = FindObjectiveTarget(context) ?? FindFallbackTarget(context);

            if (!target.HasValue)
            {
                Debug.LogWarning("ButterFly have no place to fly to");
                var emptyRepresentation = new ButterflyRepresentationData(source, null, causer.PetalType, consumedInputPositions);
                return new SkillPlan(Array.Empty<Vector2Int>(), causer, emptyRepresentation);
            }

            var representation = new ButterflyRepresentationData(source, target.Value, causer.PetalType, consumedInputPositions);
            return new SkillPlan(new[] { target.Value }, causer, representation);
        }

        private static Vector2Int? FindObjectiveTarget(SkillExecutionContext context)
        {
            var objectiveWebTargets = new HashSet<Vector2Int>();
            var objectiveMatchTargets = new HashSet<Vector2Int>();

            foreach (ObjectiveTileTargetGroup targetGroup in context.ObjectiveTargetGroups)
            {
                foreach (Vector2Int position in targetGroup.Positions)
                {
                    if (!CanAssignTarget(context, position)) continue;

                    switch (targetGroup.ObjectiveType)
                    {
                        case ObjectiveType.ClearSpiderWeb:
                            objectiveWebTargets.Add(position);
                            break;
                        case ObjectiveType.Match:
                            objectiveMatchTargets.Add(position);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(targetGroup.ObjectiveType), targetGroup.ObjectiveType, "Butterfly target priority is not defined for this objective type.");
                    }
                }
            }

            return SelectRandomTarget(new List<Vector2Int>(objectiveWebTargets), context.Random) ?? SelectRandomTarget(new List<Vector2Int>(objectiveMatchTargets), context.Random);
        }

        private static Vector2Int? FindFallbackTarget(SkillExecutionContext context)
        {
            Tile[,] grid = context.Grid;
            var obstacleTargets = new List<Vector2Int>();
            var petalTargets = new List<Vector2Int>();

            for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var position = new Vector2Int(x, y);
                if (!CanAssignTarget(context, position)) continue;

                if (grid[x, y].CanClearPetal())
                    petalTargets.Add(position);
                else
                    obstacleTargets.Add(position);
            }

            return SelectRandomTarget(obstacleTargets, context.Random) ?? SelectRandomTarget(petalTargets, context.Random);
        }

        private static Vector2Int? SelectRandomTarget(IReadOnlyList<Vector2Int> targets, System.Random random)
        {
            return targets.Count > 0 ? targets[random.Next(targets.Count)] : null;
        }

        private static bool CanAssignTarget(SkillExecutionContext context, Vector2Int position)
        {
            Tile tile = context.Grid[position.x, position.y];
            return tile != null && tile.GetClearEffectCapacity() > 0;
        }
    }
}
