using UnityEngine;

internal sealed class ViewAccessKey
{
    public PieceView View { get; }
    public Vector2Int Position { get; }
    public string UserName => Owner.Name;
    internal PieceViewScope Owner { get; }

    public ViewAccessKey(PieceView view, Vector2Int position, PieceViewScope owner)
    {
        View = view;
        Position = position;
        Owner = owner;
    }
}
