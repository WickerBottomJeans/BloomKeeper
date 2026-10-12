using System;

namespace DefaultNamespace
{
    public static class TileFactory
    {
        public static Tile Create(TileData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (data.hasEgg && (data.petalType != PetalType.None || data.skillType != SpecialSkillType.None))
                throw new ArgumentException("An egg cannot share its tile with a flower or skill.", nameof(data));

            TileFeature tileFeature = data.feature == null ? null : TileFeatureFactory.CreateTileFeature(data.feature);
            var tile = new Tile(tileFeature);
            if (!tile.CanReceivePiece() && (data.hasEgg || data.petalType != PetalType.None || data.skillType != SpecialSkillType.None))
                throw new ArgumentException("This tile cannot contain a configured piece.", nameof(data));
            return tile;
        }
    }
}
