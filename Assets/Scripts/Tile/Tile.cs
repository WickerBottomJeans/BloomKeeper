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

        /// <summary>
        /// Attempts to resolve this tile's current contents.
        /// </summary>
        /// <returns>True when a petal was removed; otherwise false.</returns>
        public abstract bool Resolve();

        public virtual bool OnAdjacentTileMatched()
        {
            return false;
        }

        public virtual string GetOverlaySpriteKey() => null;

    }
}
