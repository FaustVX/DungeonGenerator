using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DungeonGenerator.Helpers;

[Union, StructLayout(LayoutKind.Explicit)]
public readonly struct ConsoleRead : IUnion
{
    [FieldOffset(0)]
    private readonly Tag _tag;
    [FieldOffset(1)]
    private readonly ConsoleKeyInfo _consoleKeyInfo;
    [FieldOffset(1)]
    private readonly MouseEvent _mouseEvent;

    public ConsoleRead(ConsoleKeyInfo consoleKeyInfo)
    => (_tag, _consoleKeyInfo) = (Tag.ConsoleKeyInfo, consoleKeyInfo);

    public ConsoleRead(MouseEvent mouseEvent)
    => (_tag, _mouseEvent) = (Tag.MouseEvent, mouseEvent);

    public object? Value
    => _tag switch
    {
        Tag.ConsoleKeyInfo => _consoleKeyInfo,
        Tag.MouseEvent => _mouseEvent,
        _ => throw new UnreachableException(),
    };

    public bool TryGetValue(out ConsoleKeyInfo consoleKeyInfo)
    {
        consoleKeyInfo = _consoleKeyInfo;
        return _tag == Tag.ConsoleKeyInfo;
    }

    public bool TryGetValue(out MouseEvent mouseEvent)
    {
        mouseEvent = _mouseEvent;
        return _tag == Tag.MouseEvent;
    }

    private enum Tag : byte
    {
        None,
        ConsoleKeyInfo,
        MouseEvent,
    }

    public readonly record struct MouseEvent(int X, int Y, MouseButton Button, bool PressedOrMovement);

    public enum MouseButton : byte
    {
        Left = 0,
        Middle = 1,
        Right = 2,
        Movement = 35,
        ScrollUp = 64,
        ScrollDownn = 65,
    }
}
