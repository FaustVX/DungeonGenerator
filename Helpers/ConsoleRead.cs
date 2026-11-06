using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DungeonGenerator.Helpers;

[Union, StructLayout(LayoutKind.Explicit)]
public readonly struct ConsoleRead : IUnion
{
    [FieldOffset(0)]
    private readonly byte _tag = 0;
    [FieldOffset(1)]
    private readonly ConsoleKeyInfo _consoleKeyInfo;
    [FieldOffset(1)]
    private readonly MouseEvent _mouseEvent;

    public ConsoleRead(ConsoleKeyInfo consoleKeyInfo)
    => (_tag, _consoleKeyInfo) = (1, consoleKeyInfo);

    public ConsoleRead(MouseEvent mouseEvent)
    => (_tag, _mouseEvent) = (2, mouseEvent);

    public object? Value
    => _tag switch
    {
        1 => _consoleKeyInfo,
        2 => _mouseEvent,
        _ => throw new UnreachableException(),
    };

    public bool TryGetValue(out ConsoleKeyInfo consoleKeyInfo)
    {
        consoleKeyInfo = _consoleKeyInfo;
        return _tag == 1;
    }

    public bool TryGetValue(out MouseEvent mouseEvent)
    {
        mouseEvent = _mouseEvent;
        return _tag == 2;
    }

    public readonly record struct MouseEvent(MouseButton Button, int X, int Y, bool PressedOrMovement);

    public enum MouseButton
    {
        Left = 0,
        Middle = 1,
        Right = 2,
        Movement = 35,
        ScrollUp = 64,
        ScrollDownn = 65,
    }
}
