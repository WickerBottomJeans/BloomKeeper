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

        public static SkillUseResult UseSkill(Tile[,] grid, SkillActivation activation)
        {
            Petal selfPetal = new Petal(activation.SelfPetal);

            switch (activation.SkillType)
            {
                case SpecialSkillType.StripedHorizontal:
                case SpecialSkillType.StripedVertical:
                    return UseStripedSkill(grid, activation.Position, activation.SkillType, selfPetal);
                case SpecialSkillType.Bomb:
                    return UseBouquetSkill(grid, activation.Position, selfPetal);
                case SpecialSkillType.Sunburst:
                    //TODO: should we remove causer? dont seem like its needed anymore => nah. cases like stripe break a sunburst
                    PetalType targetType = activation.Combo != null ? activation.Combo.TargetPetalType : activation.CauserPetal?.PetalType ?? PetalType.None;
                    if (targetType == PetalType.None)
                        throw new InvalidOperationException("Sunburst activated with no valid target petal type.");
                    return UseSunburstSkill(grid, activation.Position, targetType, SpecialSkillType.None, selfPetal, activation);
                case SpecialSkillType.Butterfly:
                    return UseButterflySkill(grid, activation.Position, selfPetal);
                case SpecialSkillType.StripeSunburst:
                case SpecialSkillType.BouquetSunburst:
                case SpecialSkillType.ButterflySunburst:
                    if (activation.Combo == null)
                        throw new InvalidOperationException($"{activation.SkillType} activated with no ComboData.");
                    return UseSunburstSkill(grid, activation.Position, activation.Combo.TargetPetalType, activation.Combo.SunburstComboType, selfPetal, activation);
                default:
                    throw new ArgumentException("Skill not implemented.", nameof(activation.SkillType));
            }
        }

        private static SkillUseResult UseStripedSkill(
            Tile[,] grid,
            Vector2Int skillPos,
            SpecialSkillType skillType,
            Petal causer)
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
        
        public static SkillUseResult UseBouquetSkill(Tile[,] grid, Vector2Int center, Petal causer)
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
        
        public static MatchGroup UseSunburstSkill(Tile[,] grid, Vector2Int position, PetalType targetType, Petal causer)
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

        private static SkillUseResult UseSunburstSkill(Tile[,] grid, Vector2Int position, PetalType targetType, SpecialSkillType comboSkillType, Petal causer, SkillActivation activation)
        {
            if (comboSkillType == SpecialSkillType.None)
            {
                MatchGroup sunburstMatch = UseSunburstSkill(grid, position, targetType, causer);
                Vector2Int sourceA = activation.Combo != null ? activation.Combo.SourceA : activation.Position;
                Vector2Int sourceB = activation.Combo != null ? activation.Combo.SourceB : activation.Position;
                var sunburstRepresentation  = new SunburstComboRepresentationData(sourceA, sourceB, activation.EffectOrigin, new List<PetalChange>(), SpecialSkillType.Sunburst);
                return new SkillUseResult(sunburstMatch, sunburstRepresentation );
            }

            List<PetalChange> petalChanges = GiveSkillToPetalsOfType(grid, targetType, comboSkillType);

            var mutatedPositions = new List<Vector2Int>(petalChanges.Count);
            foreach (PetalChange change in petalChanges)
                mutatedPositions.Add(change.Position);

            var matchGroup = new MatchGroup(mutatedPositions, MatchShape.None, causer);
            var representation = new SunburstComboRepresentationData(activation.Combo.SourceA, activation.Combo.SourceB, activation.EffectOrigin, petalChanges, activation.SkillType);

            return new SkillUseResult(matchGroup, representation);
        }
        
        public static SkillUseResult UseButterflySkill(Tile[,] grid, Vector2Int source, Petal causer)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            var obstacleTargets = new List<Vector2Int>();
            var petalTargets = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y].HasClearableObstacle()) obstacleTargets.Add(new Vector2Int(x, y));
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
            var matchGroup = new MatchGroup(new List<Vector2Int> { target }, MatchShape.None, causer);
            var representation = new ButterflyRepresentationData(source, target);
            return new SkillUseResult(matchGroup, representation);
        }
        
        private static List<PetalChange> GiveSkillToPetalsOfType(Tile[,] grid, PetalType targetType,
            SpecialSkillType newSkill)
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
