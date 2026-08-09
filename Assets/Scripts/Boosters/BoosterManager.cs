using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public static class BoosterManager
    {
        private static readonly IReadOnlyDictionary<BoosterType, (Func<IBoosterChooser> createChooser, IBoosterExecutor executor)> Boosters = new Dictionary<BoosterType, (Func<IBoosterChooser> createChooser, IBoosterExecutor executor)>
        {
            { BoosterType.BloomWand, (() => new BloomWandChooser(), new BloomWandExecutor()) }
        };

        public static IBoosterChooser CreateChooser(BoosterType boosterType)
        {
            if (!Boosters.TryGetValue(boosterType, out var booster)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster is not registered.");
            return booster.createChooser();
        }

        public static BoosterUseResult Execute(BoosterType boosterType, Tile[,] grid, IReadOnlyList<Vector2Int> targets)
        {
            if (!Boosters.TryGetValue(boosterType, out var booster)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster is not registered.");
            return booster.executor.Execute(grid, targets);
        }
    }
}
