using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public sealed class PrismaticBloomSkillExecutor : ISkillExecutor
    {
        public SkillUseResult Execute(SkillExecutionContext context, SkillActivation activation)
        {
            if (activation.ConsumedInputs.Count > 2)
                throw new InvalidOperationException($"Prismatic Bloom requires one or two consumed inputs but received {activation.ConsumedInputs.Count}.");

            SkillParticipant? prismaticBloomParticipant = null;
            SkillParticipant? combinationPartner = null;
            foreach (SkillParticipant input in activation.ConsumedInputs)
            {
                if (input.Petal.Skill == SpecialSkillType.PrismaticBloom)
                {
                    if (prismaticBloomParticipant.HasValue)
                        throw new InvalidOperationException("Prismatic Bloom effect has more than one Prismatic Bloom consumed input.");
                    prismaticBloomParticipant = input;
                    continue;
                }

                if (combinationPartner.HasValue)
                    throw new InvalidOperationException("Prismatic Bloom effect has more than one combination input.");
                combinationPartner = input;
            }

            if (!prismaticBloomParticipant.HasValue)
                throw new InvalidOperationException("Prismatic Bloom effect has no Prismatic Bloom participant.");

            Petal targetPetal;
            SpecialSkillType replacementSkill;
            if (combinationPartner.HasValue)
            {
                targetPetal = combinationPartner.Value.Petal;
                replacementSkill = targetPetal.Skill;
            }
            else
            {
                targetPetal = activation.TriggerPetal ?? throw new InvalidOperationException("A chained Prismatic Bloom activation requires a trigger petal.");
                replacementSkill = SpecialSkillType.None;
            }

            if (targetPetal.PetalType == PetalType.None)
                throw new InvalidOperationException("Prismatic Bloom activated with no valid target petal type.");

            Petal selfPetal = new Petal(prismaticBloomParticipant.Value.Petal);
            Vector2Int source = prismaticBloomParticipant.Value.Position;
            IReadOnlyList<Vector2Int> consumedInputPositions = activation.GetConsumedInputPositions();
            MatchGroup inputMatchGroup = combinationPartner.HasValue ? new MatchGroup(new List<Vector2Int> { source }, MatchShape.None, isFromSkillCombo: true) : null;

            if (replacementSkill == SpecialSkillType.None)
            {
                MatchGroup prismaticBloomMatch = CreateMatchGroup(context.Grid, targetPetal.PetalType, selfPetal);
                var prismaticBloomRepresentation = new PrismaticBloomRepresentationData(source, replacementSkill, new List<PetalChange>(), consumedInputPositions);
                return new SkillUseResult(prismaticBloomMatch, prismaticBloomRepresentation, inputMatchGroup);
            }

            List<PetalChange> petalChanges = GiveSkillToPetalsOfType(context.Grid, targetPetal.PetalType, replacementSkill);
            var mutatedPositions = new List<Vector2Int>(petalChanges.Count);
            foreach (PetalChange change in petalChanges)
                mutatedPositions.Add(change.Position);

            var effectCauser = new Petal(targetPetal.PetalType, SpecialSkillType.PrismaticBloom);
            var matchGroup = new MatchGroup(mutatedPositions, MatchShape.None, effectCauser);
            var representation = new PrismaticBloomRepresentationData(source, replacementSkill, petalChanges, consumedInputPositions);
            return new SkillUseResult(matchGroup, representation, inputMatchGroup);
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
