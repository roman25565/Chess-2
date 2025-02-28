using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
public class HashSetPool<T> : StaticMemoryPool<HashSet<T>>
{
    public HashSetPool()
    {
        OnSpawnMethod = OnSpawned;
        OnDespawnedMethod = OnDespawned;
    }

    public static HashSetPool<T> Instance { get; } = new();

    private static void OnSpawned(HashSet<T> items)
    {
        Assert.That(items.IsEmpty());
    }

    private static void OnDespawned(HashSet<T> items)
    {
        items.Clear();
    }
}
}