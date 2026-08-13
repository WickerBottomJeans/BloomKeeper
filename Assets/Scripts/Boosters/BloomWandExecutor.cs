using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Boosters
{
    public class BloomWandExecutor : IBoosterExecutor
    {
        public BoosterUseResult Execute(Tile[,] grid, IReadOnlyList<Vector2Int> targets)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (targets.Count != 1) throw new ArgumentException("Bloom Wand requires exactly one target.", nameof(targets));

            Vector2Int target = targets[0];
            if (target.x < 0 || target.x >= grid.GetLength(0) || target.y < 0 || target.y >= grid.GetLength(1)) throw new ArgumentOutOfRangeException(nameof(targets), $"Bloom Wand target {target} is outside the board.");

            Tile tile = grid[target.x, target.y];
            if (tile == null || tile.GetClearEffectCapacity() <= 0) throw new ArgumentException($"Bloom Wand cannot clear board position {target}.", nameof(targets));

            var matchGroup = new MatchGroup(new List<Vector2Int> { target }, MatchShape.None);
            var representation = new BloomWandRepresentationData(target);
            var resolutionInput = new BoardResolutionInput(new List<MatchGroup> { matchGroup }, Array.Empty<SkillActivation>(), Array.Empty<Vector2Int>());
            return new BoosterUseResult(resolutionInput, representation);
        }
    }
}
