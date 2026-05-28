namespace DefaultNamespace
{
    //TODO: not sure if this class is justified
    public class InactiveTile : Tile
    {
        public override bool IsMatchable()
        {
            return false;
        }
        
        public override bool IsGravityAffected() => false;
        public override bool CanReceiveNewPetal() => false;
        public override void Resolve()
        {
            throw new System.NotImplementedException();
        }
    }
}