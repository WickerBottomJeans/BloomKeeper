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

            PetalType? targetPetalType = SelectTargetPetalType(combinationPartner, activation.TriggerPetal, context.Grid);
            SpecialSkillType replacementSkill = combinationPartner.HasValue ? combinationPartner.Value.Petal.Skill : SpecialSkillType.None;

            if (targetPetalType == PetalType.None)
                throw new InvalidOperationException("Prismatic Bloom activated with no valid target petal type.");

            Petal selfPetal = new Petal(prismaticBloomParticipant.Value.Petal);
            Vector2Int source = prismaticBloomParticipant.Value.Position;
            IReadOnlyList<Vector2Int> consumedInputPositions = activation.GetConsumedInputPositions();
            MatchGroup inputMatchGroup = combinationPartner.HasValue ? new MatchGroup(new List<Vector2Int> { source }, MatchShape.None, isFromSkillCombo: true) : null;

            if (!targetPetalType.HasValue)
            {
                var emptyMatch = new MatchGroup(new List<Vector2Int>(), MatchShape.None, selfPetal);
                var emptyRepresentation = new PrismaticBloomRepresentationData(source, replacementSkill, new List<PetalChange>(), consumedInputPositions);
                return new SkillUseResult(emptyMatch, emptyRepresentation, inputMatchGroup);
            }

            if (replacementSkill == SpecialSkillType.None)
            {
                MatchGroup prismaticBloomMatch = CreateMatchGroup(context.Grid, targetPetalType.Value, selfPetal);
                var prismaticBloomRepresentation = new PrismaticBloomRepresentationData(source, replacementSkill, new List<PetalChange>(), consumedInputPositions);
                return new SkillUseResult(prismaticBloomMatch, prismaticBloomRepresentation, inputMatchGroup);
            }

            List<PetalChange> petalChanges = GiveSkillToPetalsOfType(context.Grid, targetPetalType.Value, replacementSkill);
            var mutatedPositions = new List<Vector2Int>(petalChanges.Count);
            foreach (PetalChange change in petalChanges)
                mutatedPositions.Add(change.Position);

            var effectCauser = new Petal(targetPetalType.Value, SpecialSkillType.PrismaticBloom);
            var matchGroup = new MatchGroup(mutatedPositions, MatchShape.None, effectCauser);
            var representation = new PrismaticBloomRepresentationData(source, replacementSkill, petalChanges, consumedInputPositions);
            return new SkillUseResult(matchGroup, representation, inputMatchGroup);
        }

        private static PetalType? SelectTargetPetalType(SkillParticipant? combinationPartner, Petal triggerPetal, Tile[,] grid)
        {
            if (combinationPartner.HasValue) return combinationPartner.Value.Petal.PetalType;
            if (triggerPetal != null) return triggerPetal.PetalType;
            return FindMostCommonPetalType(grid);
        }

        private static PetalType? FindMostCommonPetalType(Tile[,] grid)
        {
            var counts = new Dictionary<PetalType, int>();
            var firstSeenTypes = new List<PetalType>();

            for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Petal petal = grid[x, y]?.Petal;
                if (petal == null || petal.PetalType == PetalType.None) continue;

                if (counts.TryGetValue(petal.PetalType, out int count))
                {
                    counts[petal.PetalType] = count + 1;
                    continue;
                }

                counts.Add(petal.PetalType, 1);
                firstSeenTypes.Add(petal.PetalType);
            }

            if (firstSeenTypes.Count == 0) return null;

            PetalType selectedType = firstSeenTypes[0];
            int selectedCount = counts[selectedType];
            for (int i = 1; i < firstSeenTypes.Count; i++)
            {
                PetalType candidateType = firstSeenTypes[i];
                int candidateCount = counts[candidateType];
                if (candidateCount <= selectedCount) continue;

                selectedType = candidateType;
                selectedCount = candidateCount;
            }

            return selectedType;
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
