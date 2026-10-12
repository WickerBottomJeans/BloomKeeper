#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;
using Utility;

namespace DefaultNamespace.Editor
{
    public static class EggFeatureChecks
    {
        [UnityEditor.MenuItem("Tools/BloomKeeper/Verify Egg Rules")]
        private static void RunFromMenu() => Debug.Log($"Egg feature: {Run()} checks passed.");

        public static int Run()
        {
            int passed = 0;
            void Check(bool condition, string description)
            {
                if (!condition) throw new InvalidOperationException(description);
                passed++;
            }

            Tile[,] grid = EmptyGrid(3, 3);
            grid[1, 1].SetPiece(new Egg());
            grid[2, 2] = Basket();
            Check(EggCollectionDetector.Detect(grid).Count == 0, "Diagonal baskets must not collect eggs.");
            grid[1, 2] = Basket();
            grid[2, 1] = Basket();
            var deliveries = EggCollectionDetector.Detect(grid);
            Check(deliveries.Count == 1 && deliveries[0].BasketPosition == new Vector2Int(1, 2), "Multiple baskets must choose one deterministic destination.");
            Check(grid[1, 1].Piece is Egg, "Detection must not mutate the board.");
            grid = EmptyGrid(1, 2);
            grid[0, 0].SetPiece(new Egg());
            grid[0, 1] = Basket();
            Check(EggCollectionDetector.Detect(grid).Count == 1, "Boundary detection must collect cardinal neighbors.");

            grid = EmptyGrid(3, 2);
            Egg egg = new Egg();
            grid[0, 0].SetPiece(egg);
            Petal flower = new Petal(PetalType.Daisy);
            grid[1, 0].SetPiece(flower);
            grid[2, 0] = Basket();
            grid[1, 1] = new Tile(new WebFeature(2));
            var logic = new BoardLogic(grid);
            Check(logic.HasValidMove() && ReferenceEquals(grid[0, 0].Piece, egg) && ReferenceEquals(grid[1, 0].Piece, flower), "Delivery move preview must preserve piece identity.");
            Check(logic.TryBeginSwap(Vector2Int.zero, Vector2Int.right, out var swap) && swap.RequiresResolution && swap.MatchGroups.Count == 0 && swap.EggCollections.Count == 1, "An egg delivery alone must validate a swap.");
            BoardResolution resolution = logic.Resolve(swap, _ => Array.Empty<ObjectiveTileTargetGroup>());
            Check(grid[1, 0].Piece == null && ReferenceEquals(grid[0, 0].Piece, flower), "BoardLogic must remove the delivered egg.");
            Check(resolution.EggCollections.Count == 1 && resolution.TileChanges.Count == 1 && resolution.Effects.Count == 0, "Collection must be a recorded delivery, outside flower-clear effects.");
            TileChange removal = resolution.EggCollections[0].Removal;
            Check(removal.Before.HasEgg && !removal.After.HasEgg && removal.HasAnyChange && !removal.PetalWasRemoved && !removal.FeatureChanged, "Egg removal must have an accurate immutable snapshot.");
            Check(((WebFeatureState)grid[1, 1].Feature.CaptureFeatureState()).RemainingLayers == 2, "Egg collection must not damage adjacent webs.");
            Check(logic.DetectResolution().EggCollections.Count == 0, "A removed egg cannot be collected again.");

            var eggObjective = new CollectEggObjective(new ObjectiveJson { eggsToCollect = 1 });
            var matchObjective = new MatchObjective(new ObjectiveJson { petals = new List<PetalGoal> { new PetalGoal { petalType = PetalType.Daisy, amount = 1 } } });
            var webObjective = new ClearSpiderWebObjective(new ObjectiveJson { spiderWebsToClear = 1 });
            var objectives = new ObjectiveManager(new List<IObjective> { eggObjective, matchObjective, webObjective });
            int progressEvents = 0;
            objectives.OnProgressUpdated += () => progressEvents++;
            Check(!eggObjective.CheckObjective(), "Objective progress must wait for the resolution completion event.");
            objectives.Handle(new BoardResolutionStepCompletedEvent(resolution, 0, true));
            Check(eggObjective.CheckObjective() && !matchObjective.CheckObjective() && !webObjective.CheckObjective() && progressEvents == 1, "Delivery must advance only the egg objective through the existing event pipeline.");
            Check(objectives.GetTargetGroups(BoardSnapshotBuilder.Capture(grid)).All(group => group.ObjectiveType != ObjectiveType.CollectEgg), "Destructive skills must not receive egg objective targets.");

            grid = EmptyGrid(3, 1);
            grid[0, 0].SetPiece(new Egg());
            grid[1, 0] = Basket();
            grid[2, 0].SetPiece(new Egg());
            logic = new BoardLogic(grid);
            resolution = logic.Resolve(logic.DetectResolution(), _ => Array.Empty<ObjectiveTileTargetGroup>());
            Check(resolution.EggCollections.Count == 2 && resolution.EggCollections.All(collection => collection.BasketPosition == Vector2Int.right), "One basket must accept multiple eggs in one resolution.");

            grid = EmptyGrid(2, 4);
            grid[0, 3].SetPiece(egg);
            grid[1, 1] = Basket();
            logic = new BoardLogic(grid);
            var moves = logic.ApplyGravity();
            Check(moves.Count == 1 && ReferenceEquals(grid[0, 0].Piece, egg) && logic.DetectResolution().EggCollections.Count == 0, "Passing a basket during gravity must not collect an egg.");
            grid[1, 0] = Basket();
            Check(logic.DetectResolution().EggCollections.Count == 1, "An egg beside a basket after landing must collect.");

            grid = EmptyGrid(1, 4);
            grid[0, 1] = Basket();
            grid[0, 3].SetPiece(egg);
            logic = new BoardLogic(grid);
            logic.ApplyGravity();
            Check(ReferenceEquals(grid[0, 2].Piece, egg) && grid[0, 0].Piece == null, "Baskets must block gravity through their tile.");
            logic.Fill();
            Check(grid[0, 0].Piece == null && grid[0, 1].Piece == null && ReferenceEquals(grid[0, 2].Piece, egg), "Baskets must block refill and eggs must not be overwritten.");
            grid[0, 1].ApplyClearEffect();
            grid[0, 1].HandleAdjacentTileMatched();
            Check(grid[0, 1].Feature is BasketFeature && !grid[0, 1].CanReceivePiece() && grid[0, 1].GetClearEffectCapacity() == 0, "Baskets must survive ordinary and adjacent clears.");

            grid = EmptyGrid(2, 1);
            grid[0, 0].SetPiece(egg);
            grid[1, 0].SetPiece(flower);
            logic = new BoardLogic(grid);
            logic.TryBeginSwap(Vector2Int.zero, Vector2Int.right, out swap);
            Check(!swap.RequiresResolution && ReferenceEquals(grid[0, 0].Piece, egg) && ReferenceEquals(grid[1, 0].Piece, flower), "A nonresolving mixed swap must roll back.");
            logic.BeginBooster(BoosterType.GardenersGlove, new[] { Vector2Int.zero, Vector2Int.right });
            Check(ReferenceEquals(grid[1, 0].Piece, egg), "The glove must move eggs through the shared piece slot.");
            resolution = logic.Resolve(new BoardResolutionInput(Array.Empty<MatchGroup>(), Array.Empty<SkillActivation>(), clearPositions: new[] { Vector2Int.right }), _ => Array.Empty<ObjectiveTileTargetGroup>());
            Check(ReferenceEquals(grid[1, 0].Piece, egg) && resolution.EggCollections.Count == 0 && !resolution.TileChanges.Any(change => change.HasAnyChange), "Ordinary clears must not remove or collect eggs.");
            logic.Reroll();
            Check(ReferenceEquals(grid[1, 0].Piece, egg) && !logic.CanEditPetal(Vector2Int.right), "Reroll and flower editing must preserve eggs.");
            grid[0, 0].SetPiece(new Petal(PetalType.None, SpecialSkillType.PrismaticBloom));
            Check(SkillDetector.DetectOnSwap(grid, Vector2Int.zero, Vector2Int.right).Count == 0 && !logic.HasValidMove(), "Prismatic Bloom cannot use an egg as its flower partner.");

            var level = new LevelData { levelId = 1, boardWidth = 3, boardHeight = 1, tiles = new List<TileData> { new TileData { hasEgg = true }, new TileData(), new TileData { feature = new TileFeatureData { type = TileFeatureType.Basket } } }, objectives = new List<ObjectiveJson> { new ObjectiveJson { type = ObjectiveType.CollectEgg, eggsToCollect = 1 } }, allowedBoosters = new List<BoosterType>(), starScoreThresholds = new List<StarScoreThresholdJson> { new StarScoreThresholdJson { starCount = 1, score = 10 } } };
            Check(LevelDefinitionValidator.ValidateLevelData(level).Count == 0 && BoardInitializer.Initialize(level)[0, 0].Piece is Egg, "Authored eggs and baskets must initialize from level data.");
            level.objectives[0].eggsToCollect = 2;
            Check(LevelDefinitionValidator.ValidateLevelData(level).Any(error => error.Contains("egg target")), "Impossible egg objective counts must fail authoring validation.");
            level.tiles[1].hasEgg = true;
            Check(LevelDefinitionValidator.ValidateLevelData(level).Any(error => error.Contains("beside a basket")), "Initially collectable eggs must fail authoring validation.");
            bool rejected = false;
            try { BoardInitializer.Initialize(level); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Runtime initialization must reject initially collectable eggs.");
            rejected = false;
            try { TileFactory.Create(new TileData { hasEgg = true, petalType = PetalType.Daisy }); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Authored egg and petal data cannot share a tile.");
            return passed;
        }

        private static Tile Basket() => new Tile(new BasketFeature());
        private static Tile[,] EmptyGrid(int width, int height)
        {
            var grid = new Tile[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++) grid[x, y] = new Tile();
            return grid;
        }
    }
}
#endif
