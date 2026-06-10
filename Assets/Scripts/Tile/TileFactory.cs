using System;

namespace DefaultNamespace
{
    public static class TileFactory
    {
        public static Tile Create(TileData data)
        {
            Tile tile = data.type switch
            {
                TileType.Normal   => new NormalTile(),
                TileType.Inactive => new InactiveTile(),
                TileType.Web      => new WebTile(data.webLevel),
                _                 => throw new Exception($"Unknown tile type: {data.type}")
            };
            return tile;
        }
    }
}