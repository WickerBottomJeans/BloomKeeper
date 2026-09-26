using System;

namespace DefaultNamespace
{
    public class Tile
    {
        private Petal petal;

        public bool IsPlayable { get; }
        public TileFeature Feature { get; private set; }
        public Petal Petal => petal;

        public Tile(bool isPlayable, TileFeature tileFeature = null)
        {
            if (!isPlayable && tileFeature != null) throw new ArgumentException("An inactive tile cannot contain a feature.", nameof(tileFeature));
            if (tileFeature != null && tileFeature.IsRemoved) throw new ArgumentException("A removed feature cannot be attached to a tile.", nameof(tileFeature));
            IsPlayable = isPlayable;
            Feature = tileFeature;
        }

        public void SetPetal(Petal petal)
        {
            if (petal == null) throw new ArgumentNullException(nameof(petal));
            if (!CanContainPetal()) throw new InvalidOperationException($"{GetType().Name} cannot contain a petal in its current state.");
            this.petal = petal;
        }

        public void RemovePetal() => petal = null;

        /// <summary>
        /// Whether this tile's flower can participate in a match.
        /// </summary>
        /// <returns></returns>
        public bool IsMatchable() => CanContainPetal() && Petal != null && Petal.IsMatchable();

        /// <summary>
        /// Whether flowers can fall through this tile.
        /// </summary>
        /// <returns></returns>
        public bool IsGravityAffected() => IsPlayable && (Feature == null || Feature.AllowsGravity);

        public bool CanReceiveNewPetal() => Petal == null && CanContainPetal();

        private bool CanContainPetal() => IsPlayable && (Feature == null || Feature.AllowsPetals);

        public bool CanSwapPetal() => CanContainPetal() && Petal != null;

        public bool CanClearPetal() => CanContainPetal() && Petal != null;

        public int GetClearEffectCapacity() => Feature != null ? Feature.GetFeatureClearEffectCapacity() : CanClearPetal() ? 1 : 0;

        /// <summary>
        /// Applies an effect that attempts to clear this tile's petal.
        /// </summary>
        public void ApplyClearEffect()
        {
            if (!IsPlayable) return;

            if (Feature != null)
            {
                bool absorbed = Feature.TryAbsorbClearEffect();
                RemoveCompletedFeature();
                if (absorbed) return;
            }

            if (CanClearPetal()) RemovePetal();
        }

        public void HandleAdjacentTileMatched()
        {
            if (Feature == null) return;
            Feature.HandleAdjacentTileMatched();
            RemoveCompletedFeature();
        }

        private void RemoveCompletedFeature()
        {
            if (Feature.IsRemoved) Feature = null;
        }
    }
}
