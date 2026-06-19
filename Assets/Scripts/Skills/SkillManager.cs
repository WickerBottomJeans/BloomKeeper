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
                    return CreateResult(
                        UseStripedSkill(grid, activation.Position, activation.SkillType, selfPetal));
                case SpecialSkillType.Bouquet:
                    return CreateResult(
                        UseBouquetSkill(grid, activation.Position, selfPetal));
                case SpecialSkillType.Sunburst:
                    if (activation.CauserPetal?.PetalType == null || activation.CauserPetal.PetalType == PetalType.None)
                        throw new InvalidOperationException("Sunburst activated with no valid target petal type.");
                    return CreateResult(
                        UseSunburstSkill(grid, activation.Position, activation.CauserPetal.PetalType, selfPetal));
                case SpecialSkillType.Butterfly:
                    return CreateResult(UseButterflySkill(grid, selfPetal));
                case SpecialSkillType.StripeSunburst:
                    if (activation.Combo == null)
                        throw new InvalidOperationException("StripeSunburst activated with no ComboData.");
                    return UseStripeSunburstSkill(grid, activation, selfPetal);
                default:
                    throw new ArgumentException("Skill not implemented.", nameof(activation.SkillType));
            }
        }

        private static SkillUseResult CreateResult(MatchGroup matchGroup)
        {
            return new SkillUseResult(matchGroup);
        }

        private static MatchGroup UseStripedSkill(Tile[,] grid, Vector2Int skillPos, SpecialSkillType skillType, Petal causer)
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

            return new MatchGroup(tiles, MatchShape.None, causer);
        }
        
        public static MatchGroup UseBouquetSkill(Tile[,] grid, Vector2Int center, Petal causer)
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

            return new MatchGroup(tiles, MatchShape.None, causer);
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
        
        public static MatchGroup UseButterflySkill(Tile[,] grid, Petal causer)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            var webTiles = new List<Vector2Int>();
            var allTiles = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                //TODO: why are we using webtile here??
                if (grid[x, y] is WebTile) webTiles.Add(new Vector2Int(x, y));
                else if (grid[x, y].Petal != null) allTiles.Add(new Vector2Int(x, y));
            }

            List<Vector2Int> pool = webTiles.Count > 0 ? webTiles : allTiles;
            if (pool.Count == 0) return new MatchGroup(new List<Vector2Int>(), MatchShape.None);

            Vector2Int target = pool[rng.Next(pool.Count)];
            return new MatchGroup(new List<Vector2Int> { target }, MatchShape.None, causer);
        }
        
        private static SkillUseResult UseStripeSunburstSkill(
            Tile[,] grid,
            SkillActivation activation,
            Petal causer)
        {
            PetalType targetType = activation.Combo.TargetPetalType;
            SpecialSkillType stripeDirection = activation.Combo.ComboSkillType;
            List<PetalChange> petalChanges = GiveSkillToPetalsOfType(grid, targetType, stripeDirection);

            var mutatedPositions = new List<Vector2Int>(petalChanges.Count);
            foreach (PetalChange change in petalChanges)
                mutatedPositions.Add(change.Position);

            var matchGroup = new MatchGroup(mutatedPositions, MatchShape.None, causer);
            var representation = new StripeSunburstRepresentationData(
                activation.Combo.SourceA,
                activation.Combo.SourceB,
                activation.EffectOrigin,
                petalChanges);

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
