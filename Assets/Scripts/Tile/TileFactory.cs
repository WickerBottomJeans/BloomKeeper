using System;

namespace DefaultNamespace
{
    public static class TileFactory
    {
        public static Tile Create(TileData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.isVoid)
            {
                if (data.isPlayable || data.feature != null || data.petalType != PetalType.None || data.skillType != SpecialSkillType.None)
                    throw new ArgumentException("A board hole cannot contain playable tile data.", nameof(data));
                return null;
            }

            TileFeature tileFeature = data.feature == null ? null : TileFeatureFactory.CreateTileFeature(data.feature);
            var tile = new Tile(data.isPlayable, tileFeature);
            if (!tile.CanReceiveNewPetal() && (data.petalType != PetalType.None || data.skillType != SpecialSkillType.None))
                throw new ArgumentException("This tile cannot contain configured flower or skill data.", nameof(data));
            return tile;
        }
    }
}
