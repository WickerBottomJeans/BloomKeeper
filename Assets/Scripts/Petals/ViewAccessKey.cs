using UnityEngine;

public sealed class ViewAccessKey
{
    public PetalView View { get; }
    public Vector2Int Position { get; }
    public string UserName { get; }

    public ViewAccessKey(PetalView view, Vector2Int position, string userName)
    {
        View = view;
        Position = position;
        UserName = userName;
    }
}
