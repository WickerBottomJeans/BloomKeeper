using System;

namespace DefaultNamespace
{
    public abstract class Tile
    {
        private Petal petal;

        public abstract TileType TileType { get; }
        public virtual int ObstacleLayerCount => 0;
        public Petal Petal => petal;

        public void SetPetal(Petal petal)
        {
            if (petal == null) throw new ArgumentNullException(nameof(petal));
            if (!CanContainPetal()) throw new InvalidOperationException($"{GetType().Name} cannot contain a petal in its current state.");
            this.petal = petal;
        }

        public void RemovePetal() => petal = null;

        /// <summary>
        /// Would this be consider in match detecting phase in Match detector?
        /// </summary>
        /// <returns></returns>
        public abstract bool IsMatchable();

        /// <summary>
        /// Will the petal in it be affected by gravity
        /// </summary>
        /// <returns></returns>
        public abstract bool IsGravityAffected();

        public virtual bool CanReceiveNewPetal() => Petal == null && CanContainPetal();

        protected abstract bool CanContainPetal();

        public abstract bool CanSwapPetal();

        public abstract bool CanClearPetal();

        public virtual int GetClearEffectCapacity() => CanClearPetal() ? 1 : 0;

        /// <summary>
        /// Applies an effect that attempts to clear this tile's petal.
        /// </summary>
        public abstract void ApplyClearEffect();

        public virtual void OnAdjacentTileMatched() { }
    }
}
