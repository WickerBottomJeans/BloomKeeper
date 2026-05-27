namespace DefaultNamespace
{
    public class WebTile : Tile
    {
        private int webLevel;
    
        public WebTile(int webLevel)
        {
            this.webLevel = webLevel;
        }

        public override bool IsMatchable()
        {
            return webLevel == 0 && Petal != null;
        }

        public override bool IsGravityAffected()
        {
            return webLevel == 0;
        }
        public override bool CanReceiveNewPetal() => false;
    }
}