using System;

namespace DefaultNamespace
{
    public static class TileFeatureFactory
    {
        public static TileFeature CreateTileFeature(TileFeatureData tileFeatureData)
        {
            if (tileFeatureData == null) 
                throw new ArgumentNullException(nameof(tileFeatureData));
            if (!Enum.IsDefined(typeof(TileFeatureType), tileFeatureData.type))
                throw new ArgumentOutOfRangeException(nameof(tileFeatureData), tileFeatureData.type, "Unknown tile feature.");

            return tileFeatureData.type switch
            {
                TileFeatureType.Web => new WebFeature((tileFeatureData.web ?? throw new ArgumentException("Web feature config is missing.", nameof(tileFeatureData))).layers),
                _ => throw new ArgumentOutOfRangeException(nameof(tileFeatureData), tileFeatureData.type, "Tile feature construction is not configured.")
            };
        }
    }
}
