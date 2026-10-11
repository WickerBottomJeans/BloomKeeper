using System;
using System.Collections.Generic;
using System.Linq;
using Skills;
using Boosters;
using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Owns runtime board mutations: swaps, resolution, gravity, refill, reroll, and admin edits.
    /// GameBoard sequences these operations and their presentation. Detection and skill services
    /// calculate outcomes; only this owner applies them to the live grid after initialization.
    /// </summary>
    public sealed class BoardLogic
    {
        private readonly Tile[,] boardGrid;

        public BoardLogic(Tile[,] boardGrid)
        {
            this.boardGrid = boardGrid ?? throw new ArgumentNullException(nameof(boardGrid));
        }

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

        private bool CanSwap(Vector2Int origin, Vector2Int target)
        {
            return origin != target && Contains(origin) && Contains(target)
                && boardGrid[origin.x, origin.y]?.CanSwapPetal() == true && boardGrid[target.x, target.y]?.CanSwapPetal() == true;
        }

        // False rejects the resolutionInput without mutation. True returns either a resolving move or a swap-back.
        public bool TryBeginSwap(Vector2Int origin, Vector2Int target, out BoardResolutionInput boardResolutionInput)
        {
            boardResolutionInput = null;
            if (!CanSwap(origin, target)) return false;
            boardResolutionInput = SwapAndDetect(origin, target);
            // Invalid player swaps leave the model unchanged; presentation still plays out and back.
            if (!boardResolutionInput.RequiresResolution) SwapPetals(origin, target);
            return true;
        }

        private BoardResolutionInput SwapAndDetect(Vector2Int origin, Vector2Int target)
        {
            SwapPetals(origin, target);
            try
            {
                List<SkillActivation> skillActivations = SkillDetector.DetectOnSwap(boardGrid, origin, target);
                IReadOnlyList<MatchGroup> matchGroups = skillActivations.Count > 0 ? Array.Empty<MatchGroup>() : MatchDetector.Detect(boardGrid);
                return new BoardResolutionInput(matchGroups, skillActivations, new SwapContext(origin, target));
            }
            catch
            {
                SwapPetals(origin, target);
                throw;
            }
        }

        private void SwapPetals(Vector2Int origin, Vector2Int target)
        {
            Petal petal = boardGrid[origin.x, origin.y].Petal;
            boardGrid[origin.x, origin.y].SetPetal(boardGrid[target.x, target.y].Petal);
            boardGrid[target.x, target.y].SetPetal(petal);
        }

        public List<MatchGroup> DetectMatches() => MatchDetector.Detect(boardGrid);

        public bool CanEditPetal(Vector2Int position)
        {
            if (!Contains(position)) return false;
            Tile tile = boardGrid[position.x, position.y];
            return tile != null && (tile.CanReceiveNewPetal() || tile.CanSwapPetal());
        }

        public Petal EditPetal(Vector2Int position, PetalType type, SpecialSkillType skill)
        {
            if (!CanEditPetal(position)) return null;
            var petal = new Petal(type, skill);
            boardGrid[position.x, position.y].SetPetal(petal);
            return petal;
        }

        public bool HasValidMove()
        {
            for (int x = 0; x < boardGrid.GetLength(0); x++)
            {
                for (int y = 0; y < boardGrid.GetLength(1); y++)
                {
                    if (WouldSwapResolve(new Vector2Int(x, y), new Vector2Int(x + 1, y))) return true;
                    if (WouldSwapResolve(new Vector2Int(x, y), new Vector2Int(x, y + 1))) return true;
                }
            }
            return false;
        }

        private bool WouldSwapResolve(Vector2Int origin, Vector2Int target)
        {
            if (!CanSwap(origin, target)) return false;
            Tile originTile = boardGrid[origin.x, origin.y];
            Tile targetTile = boardGrid[target.x, target.y];
            if (SkillDetector.HasActivationOnSwap(originTile.Petal.Skill, targetTile.Petal.Skill)) return true;
            if (!originTile.IsMatchable() || !targetTile.IsMatchable()) return false;

            // Only the two hypothetical cells differ. Detection reads the shared remaining cells.
            var previewGrid = (Tile[,])boardGrid.Clone();
            previewGrid[origin.x, origin.y] = new Tile(originTile.IsPlayable, originTile.Feature);
            previewGrid[target.x, target.y] = new Tile(targetTile.IsPlayable, targetTile.Feature);
            previewGrid[origin.x, origin.y].SetPetal(targetTile.Petal);
            previewGrid[target.x, target.y].SetPetal(originTile.Petal);
            List<MatchGroup> matchGroups = MatchDetector.Detect(previewGrid);
            return matchGroups.Exists(match => match.TilePositions.Contains(origin) || match.TilePositions.Contains(target));
        }

        private bool Contains(Vector2Int position) => position.x >= 0 && position.x < boardGrid.GetLength(0) && position.y >= 0 && position.y < boardGrid.GetLength(1);

        public List<(Vector2Int from, Vector2Int to)> ApplyGravity()
        {
            int columnCount = boardGrid.GetLength(0);
            int rowCount = boardGrid.GetLength(1);
            List<(Vector2Int, Vector2Int)> gravityMoves = new List<(Vector2Int, Vector2Int)>();

            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    if (boardGrid[x, y] == null || !boardGrid[x, y].CanReceiveNewPetal()) continue;

                    for (int above = y + 1; above < rowCount; above++)
                    {
                        if (boardGrid[x, above] == null || !boardGrid[x, above].IsGravityAffected()) break;
                        if (boardGrid[x, above].Petal == null) continue;

                        Petal fallingPetal = boardGrid[x, above].Petal;
                        boardGrid[x, y].SetPetal(fallingPetal);
                        boardGrid[x, above].RemovePetal();
                        gravityMoves.Add((new Vector2Int(x, above), new Vector2Int(x, y)));
                        break;
                    }
                }
            }

            return gravityMoves;
        }

        public IReadOnlyList<(Vector2Int position, Petal petal)> Fill()
        {
            int columnCount = boardGrid.GetLength(0);
            int rowCount = boardGrid.GetLength(1);
            var filledPetals = new List<(Vector2Int position, Petal petal)>();

            for (int x = 0; x < columnCount; x++)
            {
                bool foundFillEntryCandidate = false;

                for (int y = rowCount - 1; y >= 0; y--)
                {
                    Tile tile = boardGrid[x, y];

                    if (!foundFillEntryCandidate)
                    {
                        if (tile == null) continue;
                        foundFillEntryCandidate = true;
                    }

                    if (tile == null || !tile.IsGravityAffected()) break;
                    if (!tile.CanReceiveNewPetal()) continue;

                    tile.SetPetal(PetalFactory.CreateRandom());
                    filledPetals.Add((new Vector2Int(x, y), tile.Petal));
                }
            }

            filledPetals.Sort((a, b) => a.position.y.CompareTo(b.position.y));
            return filledPetals.AsReadOnly();
        }

        public IReadOnlyList<(Vector2Int position, Petal petal)> Reroll()
        {
            int columnCount = boardGrid.GetLength(0);
            int rowCount = boardGrid.GetLength(1);
            var rerolledPetals = new List<(Vector2Int position, Petal petal)>();

            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    if (boardGrid[x, y] == null || !boardGrid[x, y].IsMatchable()) continue;
                    boardGrid[x, y].SetPetal(PetalFactory.CreateRandom());
                    rerolledPetals.Add((new Vector2Int(x, y), boardGrid[x, y].Petal));
                }
            }

            return rerolledPetals.AsReadOnly();
        }

        public BoosterUseResult BeginBooster(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            switch (boosterType)
            {
                case BoosterType.GardenersGlove:
                    if (targets.Count != 2) throw new ArgumentException("Gardener's Glove requires exactly two targets.", nameof(targets));
                    if (!CanSwap(targets[0], targets[1])) throw new ArgumentException("Gardener's Glove targets cannot swap petals.", nameof(targets));
                    BoardResolutionInput gloveResolutionInput = SwapAndDetect(targets[0], targets[1]);
                    return new BoosterUseResult(gloveResolutionInput, new GardenersGloveRepresentationData(targets[0], targets[1]));
                case BoosterType.BloomWand:
                    if (targets.Count != 1) throw new ArgumentException("Bloom Wand requires exactly one target.", nameof(targets));
                    Vector2Int targetPosition = targets[0];
                    if (targetPosition.x < 0 || targetPosition.x >= boardGrid.GetLength(0) || targetPosition.y < 0 || targetPosition.y >= boardGrid.GetLength(1))
                        throw new ArgumentOutOfRangeException(nameof(targets), $"Bloom Wand target {targetPosition} is outside the board.");
                    if (boardGrid[targetPosition.x, targetPosition.y] == null || boardGrid[targetPosition.x, targetPosition.y].GetClearEffectCapacity() <= 0)
                        throw new ArgumentException($"Bloom Wand cannot clear board position {targetPosition}.", nameof(targets));
                    var clear = new BoardResolutionInput(Array.Empty<MatchGroup>(), Array.Empty<SkillActivation>(), clearPositions: new[] { targetPosition });
                    return new BoosterUseResult(clear, new BloomWandRepresentationData(targetPosition));
                default:
                    throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster is not supported.");
            }
        }

        // Calculates and applies the complete chain; presentation only consumes the returned records.
        public BoardResolution Resolve(BoardResolutionInput boardResolutionInput, Func<IReadOnlyList<TileState>, IReadOnlyList<ObjectiveTileTargetGroup>> resolveObjectiveTargets, System.Random random = null)
        {
            var effects = new List<BoardChangeGroup>();
            var tileChanges = new List<TileChange>();
            var spawnedPetals = new List<SkillPetalSpawn>();
            var skills = new List<ResolvedSkill>();
            var pendingSkills = new Queue<(SkillActivation activation, TileChange trigger)>();
            var removedChanges = new List<TileChange>();
            random ??= new System.Random();

            foreach (SkillActivation activation in boardResolutionInput.SkillActivations)
                pendingSkills.Enqueue((activation, null));

            foreach (MatchGroup match in boardResolutionInput.MatchGroups)
            {
                TryQueueSkillSpawn(match, boardGrid, boardResolutionInput.Swap, spawnedPetals);
                Record(ClearTiles(match.TilePositions, boardGrid, removedChanges, pendingSkills, matchShape: match.Shape));
            }
            if (boardResolutionInput.ClearPositions.Count > 0)
                Record(ClearTiles(boardResolutionInput.ClearPositions, boardGrid, removedChanges, pendingSkills));
            ApplyPendingSpawns(boardGrid, spawnedPetals);
            tileChanges.AddRange(ApplyAdjacentClearEffects(boardGrid, removedChanges));

            while (pendingSkills.Count > 0)
            {
                var (activation, trigger) = pendingSkills.Dequeue();
                var targets = resolveObjectiveTargets(BoardSnapshotBuilder.Capture(boardGrid));
                SkillPlan plan = SkillManager.UseSkill(new SkillExecutionContext(boardGrid, targets, random), activation);
                var transformations = new List<TileChange>();
                foreach (PetalChange change in plan.PetalChanges)
                {
                    TileState before = BoardSnapshotBuilder.CaptureTile(boardGrid, change.Position);
                    boardGrid[change.Position.x, change.Position.y].SetPetal(change.After);
                    transformations.Add(new TileChange(before, BoardSnapshotBuilder.CaptureTile(boardGrid, change.Position)));
                }

                removedChanges.Clear();
                BoardChangeGroup consumedInputs = null;
                if (plan.ConsumedPositions.Count > 0)
                {
                    consumedInputs = ClearTiles(plan.ConsumedPositions, boardGrid, removedChanges, pendingSkills, activateSkills: false);
                    Record(consumedInputs);
                }
                BoardChangeGroup impact = ClearTiles(plan.AffectedPositions, boardGrid, removedChanges, pendingSkills, plan.Causer);
                transformations.AddRange(impact.TileChanges);
                var effect = new BoardChangeGroup(transformations, impact.TriggeredSkillPositions, causingSkill: impact.CausingSkill);
                Record(effect);
                skills.Add(new ResolvedSkill(plan, effect, consumedInputs, trigger));
                tileChanges.AddRange(ApplyAdjacentClearEffects(boardGrid, removedChanges));
            }

            return new BoardResolution(effects, spawnedPetals, tileChanges, skills);

            void Record(BoardChangeGroup effect)
            {
                effects.Add(effect);
                tileChanges.AddRange(effect.TileChanges);
            }
        }

        private static void TryQueueSkillSpawn(MatchGroup match, Tile[,] boardGrid, SwapContext? swap, List<SkillPetalSpawn> pendingSpawns)
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
            bool hasSkillPetal = match.TilePositions.Any(t => boardGrid[t.x, t.y].Petal?.Skill != SpecialSkillType.None);
            if (hasSkillPetal) return;

            PetalType matchedType = boardGrid[match.TilePositions[0].x, match.TilePositions[0].y].Petal?.PetalType
                                    ?? throw new InvalidOperationException("Match group contains tile with null petal.");
            if (spawnSkill == SpecialSkillType.PrismaticBloom)
            {
                matchedType = PetalType.None;
            }
            Vector2Int spawnPos = DetermineSpawnPos(match, swap);
            pendingSpawns.Add(new SkillPetalSpawn(match.TilePositions, spawnPos, matchedType, spawnSkill.Value));
        }

        private static BoardChangeGroup ClearTiles(IReadOnlyList<Vector2Int> positions, Tile[,] boardGrid, List<TileChange> removedTileChanges, Queue<(SkillActivation activation, TileChange trigger)> pendingSkills, Petal causer = null, bool activateSkills = true, MatchShape matchShape = MatchShape.None)
        {
            var impacts = new List<TileChange>(positions.Count);
            var triggeredSkillPositions = new List<Vector2Int>();

            foreach (Vector2Int tilePosition in positions)
            {
                Tile tile = boardGrid[tilePosition.x, tilePosition.y];
                TileState before = BoardSnapshotBuilder.CaptureTile(boardGrid, tilePosition);
                if (tile != null)
                    tile.ApplyClearEffect();
                TileState after = BoardSnapshotBuilder.CaptureTile(boardGrid, tilePosition);
                var change = new TileChange(before, after);
                impacts.Add(change);

                if (!change.PetalWasRemoved) continue;
                var petal = new Petal(change.RemovedPetalType, change.RemovedSkillType);

                if (petal.Skill != SpecialSkillType.None && activateSkills)
                {
                    Petal triggerPetal = causer != null ? new Petal(causer) : null;
                    pendingSkills.Enqueue((SkillActivation.FromPetalSkill(new SkillParticipant(tilePosition, petal), triggerPetal), change));
                    triggeredSkillPositions.Add(tilePosition);
                }

                removedTileChanges.Add(change);
            }

            return new BoardChangeGroup(impacts, triggeredSkillPositions, matchShape, causer?.Skill ?? SpecialSkillType.None);
        }

        private static void ApplyPendingSpawns(Tile[,] boardGrid, List<SkillPetalSpawn> pendingSpawns)
        {
            foreach (SkillPetalSpawn spawn in pendingSpawns)
                boardGrid[spawn.SpawnPosition.x, spawn.SpawnPosition.y].SetPetal(PetalFactory.CreatePetal(spawn.PetalType, spawn.SkillType));
        }

        /// <summary>
        /// Determine spawn pos for the new skill petal
        /// </summary>
        /// <param name="match"></param>
        /// <param name="swap"></param>
        /// <returns></returns>
        private static Vector2Int DetermineSpawnPos(MatchGroup match, SwapContext? swap)
        {
            if (swap.HasValue)
            {
                if (match.TilePositions.Contains(swap.Value.Destination)) return swap.Value.Destination;
                if (match.TilePositions.Contains(swap.Value.Origin)) return swap.Value.Origin;
            }

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

        private static List<TileChange> ApplyAdjacentClearEffects(Tile[,] boardGrid, IReadOnlyList<TileChange> removedTileChanges)
        {
            int columnCount = boardGrid.GetLength(0);
            int rowCount = boardGrid.GetLength(1);
            var tileChanges = new List<TileChange>();

            HashSet<Vector2Int> notifiedTiles = new();

            foreach (TileChange removedTileChange in removedTileChanges)
            {
                foreach (Vector2Int offset in NeighborOffsets)
                {
                    Vector2Int neighborPos = removedTileChange.Position + offset;

                    if (neighborPos.x < 0 || neighborPos.x >= columnCount || neighborPos.y < 0 || neighborPos.y >= rowCount)
                        continue;

                    if (!notifiedTiles.Add(neighborPos))
                        continue;
                    //TODO: one problem, wouldnt a web get its web destroy 3 times if a stripe get exeucted
                    Tile tile = boardGrid[neighborPos.x, neighborPos.y];
                    TileState before = BoardSnapshotBuilder.CaptureTile(boardGrid, neighborPos);
                    if (tile != null)
                        tile.HandleAdjacentTileMatched();
                    TileState after = BoardSnapshotBuilder.CaptureTile(boardGrid, neighborPos);
                    var change = new TileChange(before, after, removedTileChange);
                    if (change.HasAnyChange)
                        tileChanges.Add(change);
                }
            }

            return tileChanges;
        }
    }
}
