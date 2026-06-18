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

        public override bool Resolve()
        {
            if (Petal == null) return false;

            Petal = null;
            return true;
        } 
        
        public override bool OnAdjacentTileMatched()
        {
            return TryReduceWebLevel();
        }

        private bool TryReduceWebLevel()
        {
            if (webLevel <= 0)
                return false;

            webLevel--;
            return true;
        }
        
        public override string GetOverlaySpriteKey()
        {
            if (webLevel <= 0)
                return null;
    
            return SpriteKeyHelper.GetWebOverlayKey(webLevel);
        }
    }
}
