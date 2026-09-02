using System;

namespace DefaultNamespace
{
    public class WebTile : Tile
    {
        public override TileType TileType => TileType.Web;
        public override int ObstacleLayerCount => webLevel;

        private int _webLevel;
        private int webLevel
        {
            get => _webLevel;
            set
            {
                if (value < 0) throw new InvalidOperationException("WebTile webLevel cannot be negative.");
                _webLevel = value;
            }
        }    
        public WebTile(int webLevel)
        {
            this.webLevel = webLevel;
        }

        public override bool IsMatchable()
        {
            return webLevel == 0 && Petal != null && Petal.IsMatchable();
        }

        public override bool IsGravityAffected()
        {
            return webLevel == 0;
        }

        protected override bool CanContainPetal() => webLevel == 0;

        public override bool CanSwapPetal() => webLevel == 0 && Petal != null;

        public override bool CanClearPetal() => webLevel == 0 && Petal != null;

        public override int GetClearEffectCapacity() => webLevel > 0 ? webLevel : base.GetClearEffectCapacity();

        public override void ApplyClearEffect()
        {
            if (TryReduceWebLevel())
                return;

            //Have no web to reduce aka the web tile dont have web and free to do normal petal stuff
            RemovePetal();
        } 
        
        //TODO: this doesnt look right, not controlled by boardVFX when affected by a skill. FIX THIS later :)
        public override void OnAdjacentTileMatched()
        {
            TryReduceWebLevel();
        }

        private bool TryReduceWebLevel()
        {
            if (webLevel <= 0)
                return false;

            webLevel--;
            return true;
        }
    }
}
