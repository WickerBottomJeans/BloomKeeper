namespace DefaultNamespace
{
    //TODO: not sure if this class is justified
    public class InactiveTile : Tile
    {
        public override TileType TileType => TileType.Inactive;


        public override bool IsMatchable()
        {
            return false;
        }
        
        public override bool IsGravityAffected() => false;
        public override bool CanReceiveNewPetal() => false;
        public override bool Resolve() => false;
    }
}
