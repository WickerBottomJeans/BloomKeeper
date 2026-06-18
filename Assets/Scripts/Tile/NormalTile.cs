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
        
        public override bool Resolve()
        {
            if (Petal == null) return false;

            Petal = null;
            return true;
        }
    }

}
