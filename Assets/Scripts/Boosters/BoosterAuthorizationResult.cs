using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public sealed class BoosterAuthorizationResult
    {
        public BoosterType BoosterType { get; }
        public IReadOnlyList<Vector2Int> Targets { get; }
        public bool Consumed { get; }

        public BoosterAuthorizationResult(BoosterType boosterType, IReadOnlyList<Vector2Int> targets, bool consumed)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Unsupported booster type.");
            if (targets == null) throw new ArgumentNullException(nameof(targets));

            BoosterType = boosterType;
            Targets = new List<Vector2Int>(targets).AsReadOnly();
            Consumed = consumed;
        }
    }
}
