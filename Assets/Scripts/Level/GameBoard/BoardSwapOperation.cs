using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class BoardSwapOperation
    {
        public static BoardResolutionInput Execute(Tile[,] grid, Vector2Int origin, Vector2Int target)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            ValidatePosition(grid, origin, nameof(origin));
            ValidatePosition(grid, target, nameof(target));
            if (origin == target) throw new ArgumentException("A board swap requires two distinct positions.", nameof(target));
            if (!PetalSwapper.Validate(origin, target, grid)) throw new ArgumentException($"Board positions {origin} and {target} cannot swap petals.");

            PetalSwapper.ExecuteSwapPetal(origin, target, grid);
            try
            {
                List<SkillActivation> skillActivations = SkillDetector.DetectOnSwap(grid, origin, target);
                IReadOnlyList<MatchGroup> matchGroups = skillActivations.Count > 0 ? Array.Empty<MatchGroup>() : MatchDetector.Detect(grid);
                return new BoardResolutionInput(matchGroups, skillActivations, new[] { target, origin });
            }
            catch
            {
                PetalSwapper.ExecuteSwapPetal(origin, target, grid);
                throw;
            }
        }

        private static void ValidatePosition(Tile[,] grid, Vector2Int position, string parameterName)
        {
            if (position.x < 0 || position.x >= grid.GetLength(0) || position.y < 0 || position.y >= grid.GetLength(1))
                throw new ArgumentOutOfRangeException(parameterName, position, "Board swap position is outside the board.");
        }
    }
}
