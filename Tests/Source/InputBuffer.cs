using System.Text;

namespace LanguageCore.Tests;

public sealed class InputBuffer(string buffer)
{
    int i;
    readonly byte[] _buffer = Encoding.UTF8.GetBytes(buffer);

    public byte Read()
    {
        if (i >= _buffer.Length) throw new EndOfStreamException();
        return _buffer[i++];
    }
}
