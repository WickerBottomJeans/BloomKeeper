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
        protected override bool CanContainPetal() => false;
        public override bool CanSwapPetal() => false;
        public override bool CanClearPetal() => false;
        public override void ApplyClearEffect() { }
    }
}
