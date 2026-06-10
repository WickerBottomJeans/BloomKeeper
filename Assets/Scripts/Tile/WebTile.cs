using System;

namespace DefaultNamespace
{
    public class WebTile : Tile
    {
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
            return webLevel == 0 && Petal != null && Petal.Skill != SpecialSkillType.Sunburst;
        }

        public override bool IsGravityAffected()
        {
            return webLevel == 0;
        }

        public override bool CanReceiveNewPetal()
        {
            return webLevel == 0;
        }

        public override void Resolve()
        {
            if (webLevel > 0)
            {
                webLevel--;
            } else 
            {
                Petal = null;
            }
        } 
    }
}