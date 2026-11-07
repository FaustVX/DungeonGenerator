using System.Collections;

namespace DungeonGenerator.Lib;

public class RollingList<T>(int capacity) : ICollection<T>
{
    private int _index = capacity - 1;
    private readonly T[] _items = new T[capacity];
    public int Count { get; private set => field = Math.Min(Capacity, value); } = 0;
    public bool IsReadOnly => false;
    public int Capacity { get; } = capacity;
    public bool IsFull => Count >= Capacity;
    public bool IsEmpty => Count <= 0;

    public void Add(T item)
    {
        _index = (_index + 1) % Capacity;
        Count++;
        _items[_index] = item;
    }

    public void Clear()
    => (_index, Count) = (-1, 0);

    public bool Contains(T item)
    => _items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (arrayIndex > array.Length || array.Length - arrayIndex < Count)
            throw new ArgumentException("The destination array is too small.", nameof(array));

        int start = (_index - Count + 1 + Capacity) % Capacity;
        for (int i = 0; i < Count; i++)
            array[arrayIndex + i] = _items[(start + i) % Capacity];
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return _items[(i + _index) % Count];
    }

    public bool Remove(T item)
    => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator()
    => GetEnumerator();
}
