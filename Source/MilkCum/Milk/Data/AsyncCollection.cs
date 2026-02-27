using System;
using System.Collections.Generic;

namespace MilkCum.Milk.Data;
public class AsyncCollection<T, U> : IDisposable where T : ICollection<U>
{
    private int activeSlot = 0;
    private T _collection_0;
    private T _collection_1;

    public AsyncCollection()
    {
        _collection_0 = Activator.CreateInstance<T>();
        _collection_1 = Activator.CreateInstance<T>();
    }

    public T Active => activeSlot == 0 ? _collection_0 : _collection_1;
    public T Inactive => activeSlot == 0 ? _collection_1 : _collection_0;

    public void Dispose()
    {
        activeSlot = (activeSlot + 1) % 2;
    }

}
