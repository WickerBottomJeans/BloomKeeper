using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class MatchResolver
    {

        private static readonly Vector2Int[] NeighborOffsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1)
        };

        public static MatchResolveResult Resolve(List<MatchGroup> matches, Tile[,] grid, Vector2Int swapOrigin,
            Vector2Int swapTarget)
        {
            var activations = new List<SkillActivation>();
            var cleared = new List<Vector2Int>();
            var clearedPetalTypes = new List<PetalType>();
            var pendingSpawns = new List<(Vector2Int, PetalType, SpecialSkillType)>();
            var changedTiles = new List<Vector2Int>();
            var skillComboPositions = new List<Vector2Int>();

            foreach (MatchGroup match in matches)
            {
                ProcessMatch(match, grid, swapOrigin, swapTarget, activations, cleared, clearedPetalTypes,
                    pendingSpawns, skillComboPositions);
            }

            ApplyPendingSpawns(grid, pendingSpawns);

            var allClearedForNeighborCheck = new List<Vector2Int>(cleared);
            allClearedForNeighborCheck.AddRange(skillComboPositions);
            NotifyNeighborsOfMatch(grid, allClearedForNeighborCheck, changedTiles);

            return new MatchResolveResult(cleared, clearedPetalTypes, activations, pendingSpawns, changedTiles);
        }


        private static void ProcessMatch(
            MatchGroup match,
            Tile[,] grid,
            Vector2Int swapOrigin,
            Vector2Int swapTarget,
            List<SkillActivation> activations,
            List<Vector2Int> cleared,
            List<PetalType> clearedPetalTypes,
            List<(Vector2Int, PetalType, SpecialSkillType)> pendingSpawns,
            List<Vector2Int> skillComboPositions)
        {
            TryQueueSkillSpawn(match, grid, swapOrigin, swapTarget, pendingSpawns);
            ClearMatchTiles(match, grid, activations, cleared, clearedPetalTypes, skillComboPositions);
        }

        private static void TryQueueSkillSpawn(
            MatchGroup match,
            Tile[,] grid,
            Vector2Int swapOrigin,
            Vector2Int swapTarget,
            List<(Vector2Int, PetalType, SpecialSkillType)> pendingSpawns)
        {
            SpecialSkillType? spawnSkill = match.Shape switch
            {
                MatchShape.Four => DetermineStripeSkill(match),
                MatchShape.Five => SpecialSkillType.Sunburst,
                MatchShape.TShape => SpecialSkillType.Bouquet,
                MatchShape.LShape => SpecialSkillType.Bouquet,
                MatchShape.Cross => SpecialSkillType.Bouquet,
                MatchShape.Square2x2 => SpecialSkillType.Butterfly,
                _ => null
            };

            if (!spawnSkill.HasValue) return;
            
            
            //If the match group has a skill petal in it, then execute that skill petal, not forming new skill petal
            bool hasSkillPetal = match.TilePositions.Any(t => grid[t.x, t.y].Petal?.Skill != SpecialSkillType.None);
            if (hasSkillPetal) return;

            PetalType matchedType = grid[match.TilePositions[0].x, match.TilePositions[0].y].Petal?.PetalType
                                    ?? throw new InvalidOperationException(
                                        "Match group contains tile with null petal.");
            if (spawnSkill == SpecialSkillType.Sunburst)
            {
                matchedType = PetalType.None;
            }
            Vector2Int spawnPos = DetermineSpawnPos(match, swapOrigin, swapTarget);
            pendingSpawns.Add((spawnPos, matchedType, spawnSkill.Value));
        }

        private static void ClearMatchTiles(
            MatchGroup match,
            Tile[,] grid,
            List<SkillActivation> activations,
            List<Vector2Int> cleared,
            List<PetalType> clearedPetalTypes,
            List<Vector2Int> skillComboPositions)
        {
            foreach (Vector2Int cell in match.TilePositions)
            {
                Tile tile = grid[cell.x, cell.y];
                Petal petal = tile.Petal;

                if (!tile.Resolve()) continue;

                if (petal.Skill != SpecialSkillType.None && !match.IsSkillCombo)
                    activations.Add(new SkillActivation(cell, petal.Skill, petal,
                        match.Causer != null ? new Petal(match.Causer) : null));
                clearedPetalTypes.Add(petal.PetalType);

                if (match.IsSkillCombo)
                    skillComboPositions.Add(cell);
                else
                    cleared.Add(cell);
            }
        }

        private static void ApplyPendingSpawns(Tile[,] grid,
            List<(Vector2Int pos, PetalType petalType, SpecialSkillType skill)> pendingSpawns)
        {
            foreach (var (pos, petalType, skill) in pendingSpawns)
                grid[pos.x, pos.y].Petal = PetalFactory.CreatePetal(petalType, skill);
        }

        /// <summary>
        /// Determine spawn pos for the new skill petal
        /// </summary>
        /// <param name="match"></param>
        /// <param name="swapOrigin"></param>
        /// <param name="swapTarget"></param>
        /// <returns></returns>
        private static Vector2Int DetermineSpawnPos(MatchGroup match, Vector2Int swapOrigin, Vector2Int swapTarget)
        {
            if (match.TilePositions.Contains(swapTarget)) return swapTarget;
            if (match.TilePositions.Contains(swapOrigin)) return swapOrigin;
            return GetCascadeSpawnPos(match);
        }

        private static Vector2Int GetCascadeSpawnPos(MatchGroup match)
        {
            switch (match.Shape)
            {
                case MatchShape.Four:
                case MatchShape.Five:
                    return match.TilePositions.OrderBy(t => t.x).ThenBy(t => t.y).First();
                case MatchShape.TShape:
                case MatchShape.LShape:
                case MatchShape.Cross:
                    return GetIntersectionTile(match);
                case MatchShape.Square2x2:
                    return match.TilePositions.OrderBy(t => t.y).ThenBy(t => t.x).First();

                default:
                    throw new ArgumentOutOfRangeException(nameof(match.Shape), match.Shape,
                        "No cascade spawn position defined for this shape.");
            }
        }

        private static Vector2Int GetIntersectionTile(MatchGroup match)
        {
            var xCounts = match.TilePositions.GroupBy(t => t.x).ToDictionary(g => g.Key, g => g.Count());
            var yCounts = match.TilePositions.GroupBy(t => t.y).ToDictionary(g => g.Key, g => g.Count());
            return match.TilePositions.First(t => xCounts[t.x] > 1 && yCounts[t.y] > 1);
        }

        private static SpecialSkillType DetermineStripeSkill(MatchGroup match)
        {
            return match.TilePositions[0].y == match.TilePositions[1].y
                ? SpecialSkillType.StripedHorizontal
                : SpecialSkillType.StripedVertical;
        }

        private static void NotifyNeighborsOfMatch(Tile[,] grid, List<Vector2Int> cleared,
            List<Vector2Int> changedTiles)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            HashSet<Vector2Int> notifiedTiles = new();

            foreach (Vector2Int clearedCell in cleared)
            {
                foreach (Vector2Int offset in NeighborOffsets)
                {
                    Vector2Int neighborPos = clearedCell + offset;

                    if (neighborPos.x < 0 || neighborPos.x >= cols ||
                        neighborPos.y < 0 || neighborPos.y >= rows)
                        continue;

                    if (!notifiedTiles.Add(neighborPos))
                        continue;

                    bool changed = grid[neighborPos.x, neighborPos.y].OnAdjacentTileMatched();
                    if (changed)
                        changedTiles.Add(neighborPos);
                }
            }
        }
    }
}
