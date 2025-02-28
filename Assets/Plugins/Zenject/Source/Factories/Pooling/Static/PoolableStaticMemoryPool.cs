namespace Zenject
{
public class PoolableStaticMemoryPool<TValue> : StaticMemoryPool<TValue>
    where TValue : class, IPoolable, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TValue value)
    {
        value.OnSpawned();
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}

public class PoolableStaticMemoryPool<TParam1, TValue> : StaticMemoryPool<TParam1, TValue>
    where TValue : class, IPoolable<TParam1>, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TParam1 p1, TValue value)
    {
        value.OnSpawned(p1);
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}

public class PoolableStaticMemoryPool<TParam1, TParam2, TValue> : StaticMemoryPool<TParam1, TParam2, TValue>
    where TValue : class, IPoolable<TParam1, TParam2>, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TParam1 p1, TParam2 p2, TValue value)
    {
        value.OnSpawned(p1, p2);
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}

public class
    PoolableStaticMemoryPool<TParam1, TParam2, TParam3, TValue> : StaticMemoryPool<TParam1, TParam2, TParam3, TValue>
    where TValue : class, IPoolable<TParam1, TParam2, TParam3>, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TValue value)
    {
        value.OnSpawned(p1, p2, p3);
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}

public class
    PoolableStaticMemoryPool<TParam1, TParam2, TParam3, TParam4, TValue> : StaticMemoryPool<TParam1, TParam2, TParam3,
    TParam4, TValue>
    where TValue : class, IPoolable<TParam1, TParam2, TParam3, TParam4>, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TValue value)
    {
        value.OnSpawned(p1, p2, p3, p4);
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}

public class
    PoolableStaticMemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TValue> : StaticMemoryPool<TParam1, TParam2,
    TParam3, TParam4, TParam5, TValue>
    where TValue : class, IPoolable<TParam1, TParam2, TParam3, TParam4, TParam5>, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5, TValue value)
    {
        value.OnSpawned(p1, p2, p3, p4, p5);
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}

public class
    PoolableStaticMemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TValue> : StaticMemoryPool<TParam1,
    TParam2, TParam3, TParam4, TParam5, TParam6, TValue>
    where TValue : class, IPoolable<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5, TParam6 p6, TValue value)
    {
        value.OnSpawned(p1, p2, p3, p4, p5, p6);
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}

public class
    PoolableStaticMemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TValue> : StaticMemoryPool<
    TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TValue>
    where TValue : class, IPoolable<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>, new()
{
    public PoolableStaticMemoryPool()
        : base(OnSpawned, OnDespawned)
    {
    }

    private static void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5, TParam6 p6, TParam7 p7,
        TValue value)
    {
        value.OnSpawned(p1, p2, p3, p4, p5, p6, p7);
    }

    private static void OnDespawned(TValue value)
    {
        value.OnDespawned();
    }
}
}