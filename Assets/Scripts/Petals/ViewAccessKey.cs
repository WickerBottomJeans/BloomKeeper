using UnityEngine;

internal sealed class ViewAccessKey
{
    public PetalView View { get; }
    public Vector2Int Position { get; }
    public string UserName => Owner.Name;
    internal PetalViewScope Owner { get; }

    public ViewAccessKey(PetalView view, Vector2Int position, PetalViewScope owner)
    {
        View = view;
        Position = position;
        Owner = owner;
    }
}
