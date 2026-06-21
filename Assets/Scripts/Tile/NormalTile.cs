namespace DefaultNamespace
{
    public class NormalTile : Tile
    {
        public override TileType TileType => TileType.Normal;

        public override bool IsMatchable()
        {
            return Petal != null && Petal.IsMatchable();
        }
        
        public override bool IsGravityAffected() => true;
        public override bool CanReceiveNewPetal() => Petal == null;
        
        public override TileImpactResult ApplyClearEffect()
        {
            if (Petal == null) return new TileImpactResult(null, false);

            Petal removedPetal = Petal;
            Petal = null;
            return new TileImpactResult(removedPetal, false);
        }
    }

}
