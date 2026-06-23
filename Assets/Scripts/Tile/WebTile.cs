using System;
using DefaultNamespace.Utility;

namespace DefaultNamespace
{
    public class WebTile : Tile
    {
        public override TileType TileType => TileType.Web;

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

        public override bool CanReceiveNewPetal()
        {
            return webLevel == 0 && Petal == null; ;
        }

        public override bool CanSwapPetal() => webLevel == 0 && Petal != null;

        public override bool HasClearableObstacle() => webLevel > 0;

        public override bool CanClearPetal() => webLevel == 0 && Petal != null;

        public override TileImpactResult ApplyClearEffect()
        {
            TileImpactResult webImpactResult = TryReduceWebLevel();
            
            //Just reduced web level
            if (webImpactResult.TileChanged)
            {
                return webImpactResult;
            }
            
            //Have no web to reduce aka the web tile dont have web and free to do normal petal stuff
            if (Petal == null) return new TileImpactResult(null, false);

            Petal removedPetal = Petal;
            Petal = null;
            return new TileImpactResult(removedPetal, false);
        } 
        
        //TODO: this doesnt look right, not controlled by boardVFX when affected by a skill. FIX THIS later :)
        public override TileImpactResult OnAdjacentTileMatched()
        {
            return TryReduceWebLevel();
        }

        private TileImpactResult TryReduceWebLevel()
        {
            if (webLevel <= 0)
                return new TileImpactResult(null, false);

            webLevel--;
            return new TileImpactResult(null, true, webLevel == 0);
        }
        
        public override string GetOverlaySpriteKey()
        {
            if (webLevel <= 0)
                return null;
    
            return SpriteKeyHelper.GetWebOverlayKey(webLevel);
        }
    }
}
