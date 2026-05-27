namespace DefaultNamespace
{
    public class NormalTile : Tile
    {
        public override bool IsMatchable()
        {
            return Petal != null;
        }
        
        public override bool IsGravityAffected() => true;
        public override bool CanReceiveNewPetal() => Petal == null;
    }

}