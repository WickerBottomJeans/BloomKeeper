using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public interface IBoosterChooser
    {
        UniTask<IReadOnlyList<Vector2Int>> Choose(Tile[,] grid, BoardInputHandler inputHandler);
        void Cancel();
    }
}
