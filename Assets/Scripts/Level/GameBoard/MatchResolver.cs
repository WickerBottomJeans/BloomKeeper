using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace.UI
{
    //TODO: this should only decide what hapend to the grid ... when some tile get impacted (a match some ppl say)
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

        public static MatchResolveResult Resolve(List<MatchGroup> matches, Tile[,] grid, IReadOnlyList<Vector2Int> preferredSkillSpawnPositions)
        {
            if (preferredSkillSpawnPositions == null) throw new ArgumentNullException(nameof(preferredSkillSpawnPositions));

            var groupResults = new List<MatchGroupResolveResult>(matches.Count);
            var removedPositions = new List<Vector2Int>();
            var pendingSpawns = new List<SkillPetalSpawn>();

            foreach (MatchGroup match in matches)
            {
                MatchGroupResolveResult groupResult = ProcessMatch(match, grid, preferredSkillSpawnPositions, removedPositions, pendingSpawns);
                groupResults.Add(groupResult);
            }

            ApplyPendingSpawns(grid, pendingSpawns);
            List<TileChange> adjacenttileChanges = NotifyNeighborsOfMatch(grid, removedPositions);

            return new MatchResolveResult(groupResults, pendingSpawns, adjacenttileChanges);
        }

        public static MatchResolveResult Resolve(IReadOnlyList<SkillUseResult> skillResults, Tile[,] grid, IReadOnlyList<Vector2Int> preferredSkillSpawnPositions)
        {
            var orderedMatches = new List<MatchGroup>();
            foreach (SkillUseResult skillResult in skillResults)
            {
                foreach (MatchGroup match in skillResult.GetMatchGroups())
                {
                    if (match.IsFromSkillCombo)
                        orderedMatches.Add(match);
                }
            }

            foreach (SkillUseResult skillResult in skillResults)
            {
                foreach (MatchGroup match in skillResult.GetMatchGroups())
                {
                    if (!match.IsFromSkillCombo)
                        orderedMatches.Add(match);
                }
            }

            return Resolve(orderedMatches, grid, preferredSkillSpawnPositions);
        }

        private static MatchGroupResolveResult ProcessMatch(MatchGroup match, Tile[,] grid, IReadOnlyList<Vector2Int> preferredSkillSpawnPositions, List<Vector2Int> removedPositions, List<SkillPetalSpawn> pendingSpawns)
        {
            TryQueueSkillSpawn(match, grid, preferredSkillSpawnPositions, pendingSpawns);
            return ClearMatchTiles(match, grid, removedPositions);
        }

        private static void TryQueueSkillSpawn(MatchGroup match, Tile[,] grid, IReadOnlyList<Vector2Int> preferredSkillSpawnPositions, List<SkillPetalSpawn> pendingSpawns)
        {
            SpecialSkillType? spawnSkill = match.Shape switch
            {
                MatchShape.Four => DetermineStripeSkill(match),
                MatchShape.Five => SpecialSkillType.PrismaticBloom,
                MatchShape.TShape => SpecialSkillType.Bubble,
                MatchShape.LShape => SpecialSkillType.Bubble,
                MatchShape.Cross => SpecialSkillType.Bubble,
                MatchShape.Square2x2 => SpecialSkillType.Butterfly,
                _ => null
            };

            if (!spawnSkill.HasValue) return;
            
            //If the match group has a skill petal in it, then execute that skill petal, not forming new skill petal
            bool hasSkillPetal = match.TilePositions.Any(t => grid[t.x, t.y].Petal?.Skill != SpecialSkillType.None);
            if (hasSkillPetal) return;

            PetalType matchedType = grid[match.TilePositions[0].x, match.TilePositions[0].y].Petal?.PetalType
                                    ?? throw new InvalidOperationException("Match group contains tile with null petal.");
            if (spawnSkill == SpecialSkillType.PrismaticBloom)
            {
                matchedType = PetalType.None;
            }
            Vector2Int spawnPos = DetermineSpawnPos(match, preferredSkillSpawnPositions);
            pendingSpawns.Add(new SkillPetalSpawn(match.TilePositions, spawnPos, matchedType, spawnSkill.Value));
        }

        private static MatchGroupResolveResult ClearMatchTiles(MatchGroup match, Tile[,] grid, List<Vector2Int> removedPositions)
        {
            var impacts = new List<TileChange>(match.TilePositions.Count);
            var skillActivations = new List<SkillActivation>();

            foreach (Vector2Int tilePosition in match.TilePositions)
            {
                Tile tile = grid[tilePosition.x, tilePosition.y];
                TileState before = BoardSnapshotBuilder.CaptureTile(grid, tilePosition);
                if (tile != null)
                    tile.ApplyClearEffect();
                TileState after = BoardSnapshotBuilder.CaptureTile(grid, tilePosition);
                var change = new TileChange(before, after);
                impacts.Add(change);

                if (!change.PetalWasRemoved) continue;
                var petal = new Petal(change.RemovedPetalType, change.RemovedSkillType);

                if (petal.Skill != SpecialSkillType.None && !match.IsFromSkillCombo)
                {
                    Petal triggerPetal = match.Causer != null ? new Petal(match.Causer) : null;
                    skillActivations.Add(SkillActivation.FromPetalSkill(new SkillParticipant(tilePosition, petal), triggerPetal));
                }

                removedPositions.Add(tilePosition);
            }

            return new MatchGroupResolveResult(match, impacts, skillActivations);
        }

        private static void ApplyPendingSpawns(Tile[,] grid, List<SkillPetalSpawn> pendingSpawns)
        {
            foreach (SkillPetalSpawn spawn in pendingSpawns)
                grid[spawn.SpawnPosition.x, spawn.SpawnPosition.y].SetPetal(PetalFactory.CreatePetal(spawn.PetalType, spawn.SkillType));
        }

        /// <summary>
        /// Determine spawn pos for the new skill petal
        /// </summary>
        /// <param name="match"></param>
        /// <param name="preferredSkillSpawnPositions"></param>
        /// <returns></returns>
        private static Vector2Int DetermineSpawnPos(MatchGroup match, IReadOnlyList<Vector2Int> preferredSkillSpawnPositions)
        {
            foreach (Vector2Int preferredPosition in preferredSkillSpawnPositions)
                if (match.TilePositions.Contains(preferredPosition))
                    return preferredPosition;

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

        private static List<TileChange> NotifyNeighborsOfMatch(Tile[,] grid, List<Vector2Int> cleared)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var tileChanges = new List<TileChange>();

            HashSet<Vector2Int> notifiedTiles = new();

            foreach (Vector2Int clearedTile in cleared)
            {
                foreach (Vector2Int offset in NeighborOffsets)
                {
                    Vector2Int neighborPos = clearedTile + offset;

                    if (neighborPos.x < 0 || neighborPos.x >= cols || neighborPos.y < 0 || neighborPos.y >= rows)
                        continue;

                    if (!notifiedTiles.Add(neighborPos))
                        continue;
                    //TODO: one problem, wouldnt a web get its web destroy 3 times if a stripe get exeucted
                    Tile tile = grid[neighborPos.x, neighborPos.y];
                    TileState before = BoardSnapshotBuilder.CaptureTile(grid, neighborPos);
                    if (tile != null)
                        tile.OnAdjacentTileMatched();
                    TileState after = BoardSnapshotBuilder.CaptureTile(grid, neighborPos);
                    var change = new TileChange(before, after);
                    if (change.HasAnyChange)
                        tileChanges.Add(change);
                }
            }

            return tileChanges;
        }
    }
}
