namespace LanguageCore.TUI;

#if false

public class MalbolgeRenderer
{
    readonly MalbolgeInterpreter processor;

    public MalbolgeRenderer(MalbolgeInterpreter processor)
    {
        this.processor = processor;
    }

    enum DataMovementEdge
    {
        None,
        Start,
        Middle,
        End,
    }

    readonly struct DataMovement
    {
        public readonly int Address;
        public readonly int Size;

        public DataMovement(int address, int size)
        {
            Address = address;
            Size = size;
        }

        public DataMovementEdge At(int address)
        {
            if (Size == 0) return DataMovementEdge.None;
            if (address == Address) return DataMovementEdge.Start;
            if (address == Address + Size - 1) return DataMovementEdge.End;
            if (address >= Address && address < Address + Size) return DataMovementEdge.Middle;
            return DataMovementEdge.None;
        }
    }

    Element CreateRegistersElement() => new Panel((in buffer) =>
    {
        buffer.Clear();
        BufferWriter t;

        t = buffer.Text(0, 0);
        t.Write($" C: ");
        t.PadTo(6);
        t.Write(processor.c.ToString(), AnsiColor.White);

        t = buffer.Text(0, 1);
        t.Write($" D: ");
        t.PadTo(6);
        t.Write(processor.d.ToString(), AnsiColor.White);

        t = buffer.Text(0, 2);
        t.Write($" A: ");
        t.PadTo(6);
        t.Write(processor.a.ToString(), AnsiColor.White);
    }, ElementSize.Fixed(4));

    Element CreateMemoryElement() => new Panel((in buffer) =>
    {
        buffer.Clear();

        int cols = buffer.Width / 6;
        int rows = buffer.Height / 2;

        int begin = Math.Max(0, Math.Min(processor.c / cols, processor.d / cols) - 1);

        int lastInstruction = int.MaxValue;

        for (int i = processor.c; i < processor.Memory.Length; i++)
        {
            if (!MalbolgeInterpreter.TryGetInstruction(processor.Memory, i, out char c))
            {
                lastInstruction = i - 1;
                break;
            }
            else if (c == 'v')
            {
                lastInstruction = i;
                break;
            }
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int i = x + ((y + begin) * cols);
                if (i < 0) continue;
                if (i >= processor.Memory.Length) break;

                BufferWriter t0 = buffer.Text(x * 6, y * 2);
                if (processor.d == i) t0.Write("▶", AnsiColor.BrightBlue);
                else t0.Write(' ');
                t0.Write($"{processor.Memory[i],5}", processor.d == i ? AnsiColor.BrightBlue : AnsiColor.Default);

                if (i <= lastInstruction)
                {
                    BufferWriter t1 = buffer.Text(x * 6, (y * 2) + 1);
                    if (processor.c == i) t1.Write("▶", AnsiColor.BrightMagenta);
                    else t1.Write(' ');
                    if (MalbolgeInterpreter.TryGetInstruction(processor.Memory, i, out char c))
                    {
                        t1.Write($"    {c}", processor.c == i ? AnsiColor.BrightWhite : AnsiColor.Yellow);
                    }
                }
            }
        }
    }, ElementSize.Auto());

    public void Run()
    {
        Container container = new(FlowDirection.Vertical, ElementSize.Percentage(1f), new Element[] {
            CreateMemoryElement().WithBorders("Code"),
            CreateRegistersElement().WithBorders("Registers"),
        });
        Renderer renderer = new();

        bool cancel = false;
        Console.CancelKeyPress += (sender, e) =>
        {
            cancel = true;
            e.Cancel = true;
        };
        Console.CursorVisible = false;

        int tick = 0;
        int freq = 10;

        try
        {
            while (!cancel)
            {
                while (!Console.IsOutputRedirected && Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Spacebar)
                    {
                        if (tick != int.MaxValue)
                        {
                            tick = int.MaxValue;
                        }
                        else
                        {
                            tick = 0;
                        }
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        if (tick == int.MaxValue) tick = 1;
                        else tick++;
                    }
                }

                renderer.Render(container);

                for (int i = 0; tick > 0 && i < freq; i++)
                {
                    if (!processor.Step()) goto done;
                    if (tick != int.MaxValue) tick--;
                }

                Thread.Sleep(50);
            }

        done:;
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = true;
        }
    }
}

#endif
