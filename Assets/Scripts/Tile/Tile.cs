using System;

namespace DefaultNamespace
{
    public class Tile
    {
        public TileFeature Feature { get; private set; }
        public IBoardPiece Piece { get; private set; }

        public Tile(TileFeature tileFeature = null)
        {
            if (tileFeature != null && tileFeature.IsRemoved) throw new ArgumentException("A removed feature cannot be attached to a tile.", nameof(tileFeature));
            Feature = tileFeature;
        }

        public void SetPiece(IBoardPiece piece)
        {
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            if (!CanContainPiece()) throw new InvalidOperationException($"{GetType().Name} cannot contain a piece in its current state.");
            Piece = piece;
        }

        public void RemovePiece() => Piece = null;

        /// <summary>
        /// Whether this tile's flower can participate in a match.
        /// </summary>
        /// <returns></returns>
        public bool IsMatchable() => CanContainPiece() && Piece is Petal petal && petal.IsMatchable();

        /// <summary>
        /// Whether pieces can fall through this tile.
        /// </summary>
        /// <returns></returns>
        public bool IsGravityAffected() => Feature == null || Feature.AllowsGravity;

        public bool CanReceivePiece() => Piece == null && CanContainPiece();

        private bool CanContainPiece() => Feature == null || Feature.AllowsPieces;

        public bool CanSwapPiece() => CanContainPiece() && Piece != null;

        public bool CanClearPetal() => CanContainPiece() && Piece is Petal;

        public int GetClearEffectCapacity() => Feature != null ? Feature.GetFeatureClearEffectCapacity() : CanClearPetal() ? 1 : 0;

        /// <summary>
        /// Applies an effect that attempts to clear this tile's petal.
        /// </summary>
        public void ApplyClearEffect()
        {
            if (Feature != null)
            {
                bool absorbed = Feature.TryAbsorbClearEffect();
                RemoveCompletedFeature();
                if (absorbed) return;
            }

            if (CanClearPetal()) RemovePiece();
        }

        public void HandleAdjacentTileMatched()
        {
            if (Feature == null) return;
            Feature.HandleAdjacentTileMatched();
            RemoveCompletedFeature();
        }

        private void RemoveCompletedFeature()
        {
            if (Feature.IsRemoved) Feature = null;
        }
    }
}
