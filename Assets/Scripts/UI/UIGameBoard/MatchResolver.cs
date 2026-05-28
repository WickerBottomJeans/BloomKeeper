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

            foreach (MatchGroup match in matches)
            {
                if (match.Shape == MatchShape.Four)
                {
                    PetalType matchedType = grid[match.Tiles[0].x, match.Tiles[0].y].Petal?.PetalType
                                            ?? throw new InvalidOperationException(
                                                "Match group contains tile with null petal.");
                    Vector2Int spawnPos = DetermineSpawnPos(match, swapOrigin, swapTarget);
                    pendingSpawns.Add((spawnPos, matchedType, DetermineStripeSkill(match)));
                }

                foreach (Vector2Int cell in match.Tiles)
                {
                    Petal petal = grid[cell.x, cell.y].Petal;
                    if (petal != null && petal.Skill != SpecialSkillType.None)
                        activations.Add(new SkillActivation(cell, petal.Skill));

                    grid[cell.x, cell.y].Petal = null;
                    cleared.Add(cell);
                }
            }

            foreach (var (pos, petalType, skill) in pendingSpawns)
                grid[pos.x, pos.y].Petal = PetalFactory.CreateSpecial(petalType, skill);

            return new MatchResolveResult(cleared, activations, pendingSpawns);
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
                    bool isHorizontal = match.Tiles[0].y == match.Tiles[1].y;
                    return isHorizontal
                        ? match.Tiles.OrderBy(t => t.x).First()
                        : match.Tiles.OrderBy(t => t.y).First();
                default:
                    throw new ArgumentOutOfRangeException(nameof(match.Shape), match.Shape, "No cascade spawn position defined for this shape.");
            }
        }

        private static SpecialSkillType DetermineStripeSkill(MatchGroup match)
        {
            return match.Tiles[0].y == match.Tiles[1].y
                ? SpecialSkillType.StripedHorizontal
                : SpecialSkillType.StripedVertical;
        }
    }
}