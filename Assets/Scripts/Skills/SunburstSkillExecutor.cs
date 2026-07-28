using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public sealed class SunburstSkillExecutor : ISkillExecutor
    {
        public SkillUseResult Execute(SkillExecutionContext context, SkillActivation activation)
        {
            SkillParticipant sunburstParticipant;
            SkillParticipant? combinationPartner;
            if (activation.ParticipantA.Petal.Skill == SpecialSkillType.Sunburst)
            {
                sunburstParticipant = activation.ParticipantA;
                combinationPartner = activation.ParticipantB;
            }
            else if (activation.ParticipantB.HasValue && activation.ParticipantB.Value.Petal.Skill == SpecialSkillType.Sunburst)
            {
                sunburstParticipant = activation.ParticipantB.Value;
                combinationPartner = activation.ParticipantA;
            }
            else
            {
                throw new InvalidOperationException("Sunburst effect has no Sunburst participant.");
            }

            Petal targetPetal;
            SpecialSkillType replacementSkill;
            if (combinationPartner.HasValue)
            {
                targetPetal = combinationPartner.Value.Petal;
                replacementSkill = targetPetal.Skill;
            }
            else
            {
                targetPetal = activation.TriggerPetal ?? throw new InvalidOperationException("A chained Sunburst activation requires a trigger petal.");
                replacementSkill = SpecialSkillType.None;
            }

            if (targetPetal.PetalType == PetalType.None)
                throw new InvalidOperationException("Sunburst activated with no valid target petal type.");

            Petal selfPetal = new Petal(sunburstParticipant.Petal);
            Vector2Int participantA = activation.ParticipantA.Position;
            Vector2Int? participantB = activation.ParticipantB?.Position;

            if (replacementSkill == SpecialSkillType.None)
            {
                MatchGroup sunburstMatch = CreateMatchGroup(context.Grid, targetPetal.PetalType, selfPetal);
                var sunburstRepresentation = new SunburstRepresentationData(participantA, participantB, replacementSkill, new List<PetalChange>());
                return new SkillUseResult(sunburstMatch, sunburstRepresentation);
            }

            List<PetalChange> petalChanges = GiveSkillToPetalsOfType(context.Grid, targetPetal.PetalType, replacementSkill);
            var mutatedPositions = new List<Vector2Int>(petalChanges.Count);
            foreach (PetalChange change in petalChanges)
                mutatedPositions.Add(change.Position);

            var effectCauser = new Petal(targetPetal.PetalType, SpecialSkillType.Sunburst);
            var matchGroup = new MatchGroup(mutatedPositions, MatchShape.None, effectCauser);
            var representation = new SunburstRepresentationData(participantA, participantB, replacementSkill, petalChanges);
            return new SkillUseResult(matchGroup, representation);
        }

        private static MatchGroup CreateMatchGroup(Tile[,] grid, PetalType targetType, Petal causer)
        {
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var positions = new List<Vector2Int>();

            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y]?.Petal?.PetalType == targetType)
                    positions.Add(new Vector2Int(x, y));
            }

            return new MatchGroup(positions, MatchShape.None, causer);
        }

        private static List<PetalChange> GiveSkillToPetalsOfType(Tile[,] grid, PetalType targetType, SpecialSkillType newSkill)
        {
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var changes = new List<PetalChange>();

            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y]?.Petal?.PetalType != targetType) continue;

                Petal before = grid[x, y].Petal;
                Petal after = new Petal(targetType, newSkill);
                grid[x, y].Petal = after;
                changes.Add(new PetalChange(new Vector2Int(x, y), before, after));
            }

            return changes;
        }
    }
}
