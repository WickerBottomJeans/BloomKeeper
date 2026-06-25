using DefaultNamespace;

namespace DefaultNamespace.UI
{
    public sealed class BoardCell
    {
        public Tile Tile { get; }
        public Petal Petal { get; set; }
        public bool IsVoid => Tile == null;

        public BoardCell(Tile tile, Petal petal = null)
        {
            Tile = tile;
            Petal = petal;
        }

        public static BoardCell CreateVoid() => new BoardCell(null);

        public bool IsMatchable() => Tile != null && Tile.IsMatchable(Petal);
        public bool IsGravityAffected() => Tile != null && Tile.IsGravityAffected();
        public bool CanReceiveNewPetal() => Tile != null && Tile.CanReceiveNewPetal(Petal);
        public bool CanSwapPetal() => Tile != null && Tile.CanSwapPetal(Petal);
        public bool HasClearableObstacle() => Tile != null && Tile.HasClearableObstacle();
        public bool CanClearPetal() => Tile != null && Tile.CanClearPetal(Petal);

        public TileImpactResult ApplyClearEffect()
        {
            if (Tile == null) return new TileImpactResult(null, false);

            TileImpactResult impact = Tile.ApplyClearEffect(Petal);
            if (impact.RemovedPetal != null)
                Petal = null;

            return impact;
        }

        public TileImpactResult OnAdjacentCellMatched() => Tile != null ? Tile.OnAdjacentTileMatched() : new TileImpactResult(null, false);
        public string GetOverlaySpriteKey() => Tile?.GetOverlaySpriteKey();
    }
}
