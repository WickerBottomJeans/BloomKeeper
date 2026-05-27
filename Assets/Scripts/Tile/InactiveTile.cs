namespace DefaultNamespace
{
    public class InactiveTile : Tile
    {
        public override bool IsMatchable()
        {
            return false;
        }
        
        public override bool IsGravityAffected() => false;
        public override bool CanReceiveNewPetal() => false;
    }

}