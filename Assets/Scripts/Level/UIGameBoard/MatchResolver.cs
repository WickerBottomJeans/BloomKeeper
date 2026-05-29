using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class MatchResolver
    {
        public static MatchResolveResult Resolve(List<MatchGroup> matches, Tile[,] grid, Vector2Int swapOrigin, Vector2Int swapTarget)
        {
            var activations = new List<SkillActivation>();
            var cleared = new List<Vector2Int>();
            var pendingSpawns = new List<(Vector2Int, PetalType, SpecialSkillType)>();
            var clearedPetalTypes = new List<PetalType>();

            foreach (MatchGroup match in matches)
            {
                SpecialSkillType? spawnSkill = match.Shape switch
                {
                    MatchShape.Four      => DetermineStripeSkill(match),
                    MatchShape.Five      => SpecialSkillType.Sunburst,
                    MatchShape.TShape    => SpecialSkillType.Bouquet,
                    MatchShape.LShape    => SpecialSkillType.Bouquet,
                    MatchShape.Cross     => SpecialSkillType.Bouquet,
                    MatchShape.Square2x2 => SpecialSkillType.Butterfly,
                    _                    => null
                };

                if (spawnSkill.HasValue)
                {
                    bool hasSkillPetal = match.Tiles.Any(t => grid[t.x, t.y].Petal?.Skill != SpecialSkillType.None);
                    if (!hasSkillPetal)
                    {
                        PetalType matchedType = grid[match.Tiles[0].x, match.Tiles[0].y].Petal?.PetalType
                                                ?? throw new InvalidOperationException("Match group contains tile with null petal.");
                        Vector2Int spawnPos = DetermineSpawnPos(match, swapOrigin, swapTarget);
                        pendingSpawns.Add((spawnPos, matchedType, spawnSkill.Value));
                    }
                }
                foreach (Vector2Int cell in match.Tiles)
                {
                    Tile tile = grid[cell.x, cell.y];
                    Petal petal = tile.Petal;
                    if (petal != null && petal.Skill != SpecialSkillType.None)
                        activations.Add(new SkillActivation(cell, petal.Skill, petal, match.Causer != null ? new Petal(match.Causer) : null));
                    if (tile.Petal != null)
                        clearedPetalTypes.Add(tile.Petal.PetalType);

                    tile.Resolve();

                    if (tile.Petal == null)
                        cleared.Add(cell);

                }
            }

            foreach (var (pos, petalType, skill) in pendingSpawns)
                grid[pos.x, pos.y].Petal = PetalFactory.CreateSpecial(petalType, skill);

            return new MatchResolveResult(cleared, clearedPetalTypes, activations, pendingSpawns);
        }

        private static Vector2Int DetermineSpawnPos(MatchGroup match, Vector2Int swapOrigin, Vector2Int swapTarget)
        {
            if (match.Tiles.Contains(swapTarget)) return swapTarget;
            if (match.Tiles.Contains(swapOrigin)) return swapOrigin;
            return GetCascadeSpawnPos(match);
        }
        
        private static Vector2Int GetCascadeSpawnPos(MatchGroup match)
        {
            switch (match.Shape)
            {
                case MatchShape.Four:
                case MatchShape.Five:
                    return match.Tiles.OrderBy(t => t.x).ThenBy(t => t.y).First();
                case MatchShape.TShape:
                case MatchShape.LShape:
                case MatchShape.Cross:
                    return GetIntersectionTile(match);
                case MatchShape.Square2x2:
                    return match.Tiles.OrderBy(t => t.y).ThenBy(t => t.x).First();

                default:
                    throw new ArgumentOutOfRangeException(nameof(match.Shape), match.Shape, "No cascade spawn position defined for this shape.");
            }
        }
        
        private static Vector2Int GetIntersectionTile(MatchGroup match)
        {
            var xCounts = match.Tiles.GroupBy(t => t.x).ToDictionary(g => g.Key, g => g.Count());
            var yCounts = match.Tiles.GroupBy(t => t.y).ToDictionary(g => g.Key, g => g.Count());
            return match.Tiles.First(t => xCounts[t.x] > 1 && yCounts[t.y] > 1);
        }
        
        private static SpecialSkillType DetermineStripeSkill(MatchGroup match)
        {
            return match.Tiles[0].y == match.Tiles[1].y
                ? SpecialSkillType.StripedHorizontal
                : SpecialSkillType.StripedVertical;
        }
    }
}