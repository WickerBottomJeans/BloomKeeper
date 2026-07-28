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
        public override bool CanSwapPetal() => Petal != null;
        public override bool CanClearPetal() => Petal != null;
        
        public override void ApplyClearEffect() => Petal = null;
    }
}
