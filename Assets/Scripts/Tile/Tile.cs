namespace DefaultNamespace
{
    public abstract class Tile
    {
        public abstract TileType TileType { get; }

        /// <summary>
        /// Would this be consider in match detecting phase in Match detector?
        /// </summary>
        /// <returns></returns>
        public abstract bool IsMatchable(Petal petal);

        /// <summary>
        /// Will the petal in it be affected by gravity
        /// </summary>
        /// <returns></returns>
        public abstract bool IsGravityAffected();

        public abstract bool CanReceiveNewPetal(Petal petal);

        public abstract bool CanSwapPetal(Petal petal);

        public abstract bool CanClearPetal(Petal petal);

        public virtual int GetClearEffectCapacity(Petal petal) => CanClearPetal(petal) ? 1 : 0;

        /// <summary>
        /// Applies an effect that attempts to clear this tile's petal.
        /// </summary>
        public abstract TileImpactResult ApplyClearEffect(Petal petal);

        public virtual TileImpactResult OnAdjacentTileMatched()
        {
            return new TileImpactResult(null, false);
        }

        public virtual string GetOverlaySpriteKey() => null;
    }
}
