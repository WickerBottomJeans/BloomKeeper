namespace DefaultNamespace
{
    public abstract class Tile
    {
        public Petal Petal { get; set; }
        public abstract TileType TileType { get; }

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

        public abstract bool CanReceiveNewPetal();

        public abstract bool CanSwapPetal();

        public abstract bool HasClearableObstacle();

        public abstract bool CanClearPetal();

        /// <summary>
        /// Applies an effect that attempts to clear this tile's petal.
        /// </summary>
        public abstract TileImpactResult ApplyClearEffect();

        public virtual bool OnAdjacentTileMatched()
        {
            return false;
        }

        public virtual string GetOverlaySpriteKey() => null;

    }
}
