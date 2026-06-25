using DefaultNamespace;

namespace DefaultNamespace.UI
{
    public sealed class BoardCell
    {
        public bool IsVoid { get; }
        public Tile Tile { get; }
        public Petal Petal { get; set; }

        public BoardCell(bool isVoid, Tile tile, Petal petal = null)
        {
            IsVoid = isVoid;
            Tile = tile;
            Petal = petal;
        }

        public bool IsMatchable() => !IsVoid && Tile != null && Tile.IsMatchable(Petal);
        public bool IsFillEntryCandidate() => !IsVoid;
        public bool IsGravityAffected() => Tile != null && Tile.IsGravityAffected();
        public bool CanReceiveNewPetal() => !IsVoid && Tile != null && Tile.CanReceiveNewPetal(Petal);
        public bool CanSwapPetal() => !IsVoid && Tile != null && Tile.CanSwapPetal(Petal);
        public bool HasClearableObstacle() => !IsVoid && Tile != null && Tile.HasClearableObstacle();
        public bool CanClearPetal() => !IsVoid && Tile != null && Tile.CanClearPetal(Petal);

        public TileImpactResult ApplyClearEffect()
        {
            if (IsVoid || Tile == null) return new TileImpactResult(null, false);

            TileImpactResult impact = Tile.ApplyClearEffect(Petal);
            if (impact.RemovedPetal != null)
                Petal = null;

            return impact;
        }

        public TileImpactResult OnAdjacentCellMatched() => !IsVoid && Tile != null ? Tile.OnAdjacentTileMatched() : new TileImpactResult(null, false);
        public string GetOverlaySpriteKey() => !IsVoid ? Tile?.GetOverlaySpriteKey() : null;
    }
}
