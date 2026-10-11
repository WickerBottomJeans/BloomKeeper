using System;
using System.Collections.Generic;
using UnityEngine;

// Owns the petal views used by one animation. Disposing releases every remaining view.
public sealed class PetalViewScope : IDisposable
{
    private readonly PetalViewManager manager;
    private readonly Dictionary<Vector2Int, ViewAccessKey> keys = new();
    public string Name { get; }
    internal ViewAccessKey this[Vector2Int position] => keys[position];
    public IEnumerable<Vector2Int> Keys => keys.Keys;

    public PetalViewScope(PetalViewManager manager, string name)
    {
        this.manager = manager;
        Name = name;
    }

    public bool Owns(Vector2Int position) => keys.ContainsKey(position);
    internal bool TryGetValue(Vector2Int position, out ViewAccessKey key) => keys.TryGetValue(position, out key);
    internal void Add(Vector2Int position, ViewAccessKey key) => keys.Add(position, key);
    internal void Remove(Vector2Int position) => keys.Remove(position);

    public bool TryAcquire(Vector2Int position) => Owns(position) || manager.TryAcquireView(position, this, out _);

    public void AcquireRequired(Vector2Int position)
    {
        if (!TryAcquire(position)) throw new InvalidOperationException($"Petal view at {position} cannot be acquired by {Name}.");
    }

    public List<Vector2Int> AcquireAvailable(IEnumerable<Vector2Int> positions)
    {
        var acquired = new List<Vector2Int>();
        foreach (Vector2Int position in positions)
            if (TryAcquire(position)) acquired.Add(position);
        return acquired;
    }

    public void Release(Vector2Int position)
    {
        if (keys.TryGetValue(position, out ViewAccessKey key)) manager.ReleaseView(key);
    }

    internal void TakeOver(Vector2Int position) => manager.TransferView(position, this);

    public void Dispose()
    {
        foreach (ViewAccessKey key in new List<ViewAccessKey>(keys.Values)) manager.ReleaseView(key);
    }
}
