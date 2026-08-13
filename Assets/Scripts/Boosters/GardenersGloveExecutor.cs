using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Boosters
{
    public class GardenersGloveExecutor : IBoosterExecutor
    {
        public BoosterUseResult Execute(Tile[,] grid, IReadOnlyList<Vector2Int> targets)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (targets.Count != 2) throw new ArgumentException("Gardener's Glove requires exactly two targets.", nameof(targets));

            Vector2Int origin = targets[0];
            Vector2Int target = targets[1];
            BoardResolutionInput resolutionInput = BoardSwapOperation.Execute(grid, origin, target);
            var representation = new GardenersGloveRepresentationData(origin, target);
            return new BoosterUseResult(resolutionInput, representation);
        }
    }
}
