namespace DefaultNamespace
{
    public abstract class Tile
    {
        public Petal Petal { get; set; }
        
        public abstract bool IsMatchable();
        
        /// <summary>
        /// Will the petal in it be affected by gravity
        /// </summary>
        /// <returns></returns>
        public abstract bool IsGravityAffected();
        public abstract bool CanReceiveNewPetal();
        
        public abstract void Resolve();

    }

}