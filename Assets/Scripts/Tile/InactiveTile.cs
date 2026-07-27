namespace DefaultNamespace
{
    //TODO: not sure if this class is justified
    public class InactiveTile : Tile
    {
        public override TileType TileType => TileType.Inactive;

        public override bool IsMatchable(Petal petal)
        {
            return false;
        }
        
        public override bool IsGravityAffected() => false;
        public override bool CanReceiveNewPetal(Petal petal) => false;
        public override bool CanSwapPetal(Petal petal) => false;
        public override bool CanClearPetal(Petal petal) => false;
        public override TileImpactResult ApplyClearEffect(Petal petal) => new TileImpactResult(null, false);
    }
}
