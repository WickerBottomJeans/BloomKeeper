namespace DefaultNamespace
{
    public class NormalTile : Tile
    {
        public override bool IsMatchable()
        {
            return Petal != null && Petal.Skill != SpecialSkillType.Sunburst;
        }
        
        public override bool IsGravityAffected() => true;
        public override bool CanReceiveNewPetal() => Petal == null;
        
        public override void Resolve()
        {
            Petal = null;
        }

        public override void OnAdjacentTileMatched()
        {
        }
    }

}