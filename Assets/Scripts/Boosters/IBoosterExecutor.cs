using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public interface IBoosterExecutor
    {
        BoosterUseResult Execute(Tile[,] grid, IReadOnlyList<Vector2Int> targets);
    }
}
