namespace DefaultNamespace
{
    public abstract class Tile
    {
        public abstract TileType TileType { get; }
        public virtual int ObstacleLayerCount => 0;
        public Petal Petal { get; set; }

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

        public abstract bool CanClearPetal();

        public virtual int GetClearEffectCapacity() => CanClearPetal() ? 1 : 0;

        /// <summary>
        /// Applies an effect that attempts to clear this tile's petal.
        /// </summary>
        public abstract void ApplyClearEffect();

        public virtual void OnAdjacentTileMatched() { }
    }
}
