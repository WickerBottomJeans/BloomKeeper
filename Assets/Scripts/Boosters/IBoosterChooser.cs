using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public interface IBoosterChooser
    {
        event Action<Vector2Int, bool> TargetSelectionChanged;

        IReadOnlyList<Vector2Int> GetBoosterTargetCandidates(Tile[,] grid);
        UniTask<BoosterTargetSelectionResult> Choose(Tile[,] grid, BoardInputHandler inputHandler);
        void Cancel();
    }
}
