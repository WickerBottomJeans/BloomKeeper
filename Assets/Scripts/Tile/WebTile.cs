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

        public override bool IsMatchable(Petal petal)
        {
            return webLevel == 0 && petal != null && petal.IsMatchable();
        }

        public override bool IsGravityAffected()
        {
            return webLevel == 0;
        }

        public override bool CanReceiveNewPetal(Petal petal)
        {
            return webLevel == 0 && petal == null;
        }

        public override bool CanSwapPetal(Petal petal) => webLevel == 0 && petal != null;

        public override bool HasClearableObstacle() => webLevel > 0;

        public override bool CanClearPetal(Petal petal) => webLevel == 0 && petal != null;

        public override TileImpactResult ApplyClearEffect(Petal petal)
        {
            TileImpactResult webImpactResult = TryReduceWebLevel();
            
            //Just reduced web level
            if (webImpactResult.TileChanged)
            {
                return webImpactResult;
            }
            
            //Have no web to reduce aka the web tile dont have web and free to do normal petal stuff
            return petal != null ? new TileImpactResult(petal, false) : new TileImpactResult(null, false);
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
