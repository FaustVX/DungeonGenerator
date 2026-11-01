namespace DungeonGenerator.Lib;

public class DefaultableDictionary<TKey, TValue>(int capacity, Func<TKey, TValue> create) : Dictionary<TKey, TValue>(capacity), IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TKey, TValue> _defaultValue = create;

    public DefaultableDictionary(Func<TKey, TValue> create)
        : this(4, create) {}

    public new TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;
            return base[key] = _defaultValue(key);
        }
        set => base[key] = value;
    }

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get => this[key];
        set => this[key] = value;
    }

    TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
    => this[key];
}
