using UnityEngine;

namespace DefaultNamespace
{
    public sealed class EggCollection
    {
        public TileChange Removal { get; }
        public Vector2Int EggPosition => Removal.Position;
        public Vector2Int BasketPosition { get; }

        public EggCollection(TileChange removal, Vector2Int basketPosition)
        {
            Removal = removal;
            BasketPosition = basketPosition;
        }
    }
}
