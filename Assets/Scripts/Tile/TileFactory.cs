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

            if (data.type != TileType.Inactive)
                tile.Petal = PetalFactory.CreateForTileMap(data);

            return tile;
        }
    }
}