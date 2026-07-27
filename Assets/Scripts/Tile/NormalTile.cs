namespace DefaultNamespace
{
    public class NormalTile : Tile
    {
        public override TileType TileType => TileType.Normal;

        public override bool IsMatchable(Petal petal)
        {
            return petal != null && petal.IsMatchable();
        }
        
        public override bool IsGravityAffected() => true;
        public override bool CanReceiveNewPetal(Petal petal) => petal == null;
        public override bool CanSwapPetal(Petal petal) => petal != null;
        public override bool CanClearPetal(Petal petal) => petal != null;
        
        public override TileImpactResult ApplyClearEffect(Petal petal)
        {
            return petal != null ? new TileImpactResult(petal, false) : new TileImpactResult(null, false);
        }
    }
}
