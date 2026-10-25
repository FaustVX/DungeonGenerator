namespace DungeonGenerator.Lib;

public class DefaultableList<TValue>(Func<TValue> create, int capacity) : List<TValue>(capacity), IList<TValue>, IReadOnlyList<TValue>
{
    public DefaultableList(Func<TValue> create)
    : this(create, 4) { }
    private readonly Func<TValue> _defaultValue = create;

    public TValue this[Index index]
    {
        get
        {
            if (index.GetOffset(Count) >= Count)
                Add(_defaultValue());
            return base[index];
        }
        set => base[index] = value;
    }

    TValue IList<TValue>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    TValue IReadOnlyList<TValue>.this[int index] => this[index];
}
