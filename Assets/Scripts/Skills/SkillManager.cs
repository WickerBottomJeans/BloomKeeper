using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public static class SkillManager
    {
        private static readonly System.Random rng = new System.Random();

        public const int BouquetRange = 1;

        public static List<SkillUseResult> UseSkills(BoardCell[,] grid, IReadOnlyList<SkillActivation> activations)
        {
            var results = new List<SkillUseResult>(activations.Count);
            var reservedButterflyObstacleTargets = new HashSet<Vector2Int>();

            foreach (SkillActivation activation in activations)
                results.Add(UseSkill(grid, activation, reservedButterflyObstacleTargets));

            return results;
        }

        private static SkillUseResult UseSkill(BoardCell[,] grid, SkillActivation activation, HashSet<Vector2Int> reservedButterflyObstacleTargets)
        {
            switch (activation.EffectType)
            {
                case SpecialSkillType.StripedHorizontal:
                case SpecialSkillType.StripedVertical:
                    return UseStripedSkill(grid, activation.ParticipantA.Position, activation.EffectType, new Petal(activation.ParticipantA.Petal));
                case SpecialSkillType.Bomb:
                    return UseBouquetSkill(grid, activation.ParticipantA.Position, new Petal(activation.ParticipantA.Petal));
                case SpecialSkillType.Sunburst:
                case SpecialSkillType.StripeSunburst:
                case SpecialSkillType.BouquetSunburst:
                case SpecialSkillType.ButterflySunburst:
                    return UseSunburstSkill(grid, activation);
                case SpecialSkillType.Butterfly:
                    return UseButterflySkill(grid, activation.ParticipantA.Position, new Petal(activation.ParticipantA.Petal), reservedButterflyObstacleTargets);
                default:
                    throw new ArgumentException("Skill not implemented.", nameof(activation.EffectType));
            }
        }

        private static SkillUseResult UseStripedSkill(BoardCell[,] grid, Vector2Int skillPos, SpecialSkillType skillType, Petal causer)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var tiles = new List<Vector2Int>();

            if (skillType == SpecialSkillType.StripedHorizontal)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (grid[x, skillPos.y].Petal == null) continue;
                    tiles.Add(new Vector2Int(x, skillPos.y));
                }
            }
            else if (skillType == SpecialSkillType.StripedVertical)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (grid[skillPos.x, y].Petal == null) continue;
                    tiles.Add(new Vector2Int(skillPos.x, y));
                }
            }
            else
            {
                throw new ArgumentException("Not a striped skill type.", nameof(skillType));
            }

            var matchGroup = new MatchGroup(tiles, MatchShape.None, causer);
            var representation = new StripedRepresentationData(skillPos, skillType, tiles);
            return new SkillUseResult(matchGroup, representation);
        }
        
        public static SkillUseResult UseBouquetSkill(BoardCell[,] grid, Vector2Int center, Petal causer)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var tiles = new List<Vector2Int>();

            for (int x = center.x - BouquetRange; x <= center.x + BouquetRange; x++)
            for (int y = center.y - BouquetRange; y <= center.y + BouquetRange; y++)
            {
                if (x < 0 || x >= cols || y < 0 || y >= rows) continue;
                tiles.Add(new Vector2Int(x, y));
            }

            var matchGroup = new MatchGroup(tiles, MatchShape.None, causer);
            var representation = new BouquetRepresentationData(center, tiles);
            return new SkillUseResult(matchGroup, representation);
        }
        
        private static MatchGroup CreateSunburstMatchGroup(BoardCell[,] grid, PetalType targetType, Petal causer)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var tiles = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y].Petal?.PetalType == targetType)
                    tiles.Add(new Vector2Int(x, y));
            }

            return new MatchGroup(tiles, MatchShape.None, causer);
        }

        private static SkillUseResult UseSunburstSkill(BoardCell[,] grid, SkillActivation activation)
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
                MatchGroup sunburstMatch = CreateSunburstMatchGroup(grid, targetPetal.PetalType, selfPetal);
                var sunburstRepresentation = new SunburstRepresentationData(participantA, participantB, replacementSkill, new List<PetalChange>());
                return new SkillUseResult(sunburstMatch, sunburstRepresentation);
            }

            List<PetalChange> petalChanges = GiveSkillToPetalsOfType(grid, targetPetal.PetalType, replacementSkill);

            var mutatedPositions = new List<Vector2Int>(petalChanges.Count);
            foreach (PetalChange change in petalChanges)
                mutatedPositions.Add(change.Position);

            var effectCauser = new Petal(targetPetal.PetalType, activation.EffectType);
            var matchGroup = new MatchGroup(mutatedPositions, MatchShape.None, effectCauser);
            var representation = new SunburstRepresentationData(participantA, participantB, replacementSkill, petalChanges);

            return new SkillUseResult(matchGroup, representation);
        }
        
        private static SkillUseResult UseButterflySkill(BoardCell[,] grid, Vector2Int source, Petal causer, HashSet<Vector2Int> reservedObstacleTargets)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            var obstacleTargets = new List<Vector2Int>();
            var petalTargets = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y].HasClearableObstacle())
                {
                    var position = new Vector2Int(x, y);
                    if (!reservedObstacleTargets.Contains(position)) obstacleTargets.Add(position);
                }
                else if (grid[x, y].CanClearPetal()) petalTargets.Add(new Vector2Int(x, y));
            }

            List<Vector2Int> pool = obstacleTargets.Count > 0 ? obstacleTargets : petalTargets;
            if (pool.Count == 0)
            {
                //TODO: test this case
                Debug.LogWarning("ButterFly have no place to fly to");
                var emptyMatch = new MatchGroup(new List<Vector2Int>(), MatchShape.None, causer);
                var emptyRepresentation = new ButterflyRepresentationData(source, null);
                return new SkillUseResult(emptyMatch, emptyRepresentation);
            }

            Vector2Int target = pool[rng.Next(pool.Count)];
            if (obstacleTargets.Count > 0) reservedObstacleTargets.Add(target);
            var matchGroup = new MatchGroup(new List<Vector2Int> { target }, MatchShape.None, causer);
            var representation = new ButterflyRepresentationData(source, target);
            return new SkillUseResult(matchGroup, representation);
        }
        
        private static List<PetalChange> GiveSkillToPetalsOfType(BoardCell[,] grid, PetalType targetType, SpecialSkillType newSkill)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var changes = new List<PetalChange>();

            for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y].Petal?.PetalType != targetType) continue;

                Petal before = grid[x, y].Petal;
                Petal after = new Petal(targetType, newSkill);
                grid[x, y].Petal = after;
                changes.Add(new PetalChange(new Vector2Int(x, y), before, after));
            }

            return changes;
        }
    }
}
