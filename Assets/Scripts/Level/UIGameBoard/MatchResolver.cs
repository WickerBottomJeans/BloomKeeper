using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace.UI
{
    //TODO: this should only decide what hapend to the grid ... when some cell get impacted (a match some ppl say)
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

        public static MatchResolveResult Resolve(List<MatchGroup> matches, BoardCell[,] grid, Vector2Int swapOrigin, Vector2Int swapTarget)
        {
            var activations = new List<SkillActivation>();
            var groupResults = new List<MatchGroupResolveResult>(matches.Count);
            var removedPositions = new List<Vector2Int>();
            var clearedPetalTypes = new List<PetalType>();
            var pendingSpawns = new List<SkillPetalSpawn>();
            var adjacentTileChanges = new List<Vector2Int>();
            int cleanedSpiderWebTileCount = 0;

            foreach (MatchGroup match in matches)
            {
                MatchGroupResolveResult groupResult = ProcessMatch(match, grid, swapOrigin, swapTarget, activations, removedPositions, clearedPetalTypes, pendingSpawns);
                groupResults.Add(groupResult);
                foreach (var impact in groupResult.Impacts)
                {
                    if (impact.Outcome.SpiderWebCleaned)
                        cleanedSpiderWebTileCount++;
                }
            }

            ApplyPendingSpawns(grid, pendingSpawns);

            cleanedSpiderWebTileCount += NotifyNeighborsOfMatch(grid, removedPositions, adjacentTileChanges);

            return new MatchResolveResult(groupResults, clearedPetalTypes, activations, pendingSpawns, adjacentTileChanges, cleanedSpiderWebTileCount);
        }

        private static MatchGroupResolveResult ProcessMatch(MatchGroup match, BoardCell[,] grid, Vector2Int swapOrigin, Vector2Int swapTarget, List<SkillActivation> activations, List<Vector2Int> removedPositions, List<PetalType> clearedPetalTypes, List<SkillPetalSpawn> pendingSpawns)
        {
            TryQueueSkillSpawn(match, grid, swapOrigin, swapTarget, pendingSpawns);
            return ClearMatchTiles(match, grid, activations, removedPositions, clearedPetalTypes);
        }

        private static void TryQueueSkillSpawn(MatchGroup match, BoardCell[,] grid, Vector2Int swapOrigin, Vector2Int swapTarget, List<SkillPetalSpawn> pendingSpawns)
        {
            SpecialSkillType? spawnSkill = match.Shape switch
            {
                MatchShape.Four => DetermineStripeSkill(match),
                MatchShape.Five => SpecialSkillType.Sunburst,
                MatchShape.TShape => SpecialSkillType.Bomb,
                MatchShape.LShape => SpecialSkillType.Bomb,
                MatchShape.Cross => SpecialSkillType.Bomb,
                MatchShape.Square2x2 => SpecialSkillType.Butterfly,
                _ => null
            };

            if (!spawnSkill.HasValue) return;
            
            //If the match group has a skill petal in it, then execute that skill petal, not forming new skill petal
            bool hasSkillPetal = match.TilePositions.Any(t => grid[t.x, t.y].Petal?.Skill != SpecialSkillType.None);
            if (hasSkillPetal) return;

            PetalType matchedType = grid[match.TilePositions[0].x, match.TilePositions[0].y].Petal?.PetalType
                                    ?? throw new InvalidOperationException("Match group contains tile with null petal.");
            if (spawnSkill == SpecialSkillType.Sunburst)
            {
                matchedType = PetalType.None;
            }
            Vector2Int spawnPos = DetermineSpawnPos(match, swapOrigin, swapTarget);
            pendingSpawns.Add(new SkillPetalSpawn(match.TilePositions, spawnPos, matchedType, spawnSkill.Value));
        }

        private static MatchGroupResolveResult ClearMatchTiles(MatchGroup match, BoardCell[,] grid, List<SkillActivation> activations, List<Vector2Int> removedPositions, List<PetalType> clearedPetalTypes)
        {
            var impacts = new List<(Vector2Int Position, TileImpactResult Outcome)>(match.TilePositions.Count);
            var triggeredSkillPositions = new List<Vector2Int>();

            foreach (Vector2Int cellPosition in match.TilePositions)
            {
                BoardCell cell = grid[cellPosition.x, cellPosition.y];
                TileImpactResult impactResult = cell.ApplyClearEffect();
                impacts.Add((cellPosition, impactResult));

                Petal petal = impactResult.RemovedPetal;
                if (petal == null) continue;

                if (petal.Skill != SpecialSkillType.None && !match.IsFromSkillCombo)
                {
                    Petal triggerPetal = match.Causer != null ? new Petal(match.Causer) : null;
                    activations.Add(new SkillActivation(petal.Skill, new SkillParticipant(cellPosition, petal), triggerPetal: triggerPetal));
                    triggeredSkillPositions.Add(cellPosition);
                }

                removedPositions.Add(cellPosition);
                clearedPetalTypes.Add(petal.PetalType);
            }

            return new MatchGroupResolveResult(match, impacts, triggeredSkillPositions);
        }

        private static void ApplyPendingSpawns(BoardCell[,] grid, List<SkillPetalSpawn> pendingSpawns)
        {
            foreach (SkillPetalSpawn spawn in pendingSpawns)
                grid[spawn.SpawnPosition.x, spawn.SpawnPosition.y].Petal = PetalFactory.CreatePetal(spawn.PetalType, spawn.SkillType);
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
                    throw new ArgumentOutOfRangeException(nameof(match.Shape), match.Shape, "No cascade spawn position defined for this shape.");
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

        private static int NotifyNeighborsOfMatch(BoardCell[,] grid, List<Vector2Int> cleared, List<Vector2Int> changedTiles)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            int cleanedSpiderWebTileCount = 0;

            HashSet<Vector2Int> notifiedTiles = new();

            foreach (Vector2Int clearedCell in cleared)
            {
                foreach (Vector2Int offset in NeighborOffsets)
                {
                    Vector2Int neighborPos = clearedCell + offset;

                    if (neighborPos.x < 0 || neighborPos.x >= cols || neighborPos.y < 0 || neighborPos.y >= rows)
                        continue;

                    if (!notifiedTiles.Add(neighborPos))
                        continue;
                    //TODO: one problem, wouldnt a web get its web destroy 3 times if a stripe get exeucted
                    TileImpactResult impactResult = grid[neighborPos.x, neighborPos.y].OnAdjacentCellMatched();
                    if (impactResult.TileChanged)
                        changedTiles.Add(neighborPos);
                    if (impactResult.SpiderWebCleaned)
                        cleanedSpiderWebTileCount++;
                }
            }

            return cleanedSpiderWebTileCount;
        }
    }
}
