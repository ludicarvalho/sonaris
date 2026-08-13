namespace Sonaris.Domain.DTOs.Music.Metadata.Readers;

/// <summary>
/// Leitor incremental sobre um Stream, mantendo a leitura apenas dos bytes necessários.
/// </summary>
public sealed class FileByteReader(Stream stream) : IDisposable
{
    private readonly Stream stream = stream ?? throw new ArgumentNullException(nameof(stream));

    public long Position
    {
        get => stream.Position;
        set => stream.Position = value;
    }

    internal long Length => stream.Length;

    internal byte ReadByte()
    {
        int value = stream.ReadByte();

        if (value < 0)
            throw new EndOfStreamException();

        return (byte)value;
    }

    internal void ReadExactly(Span<byte> buffer)
        => stream.ReadExactly(buffer);

    internal byte[] ReadBytes(int count)
    {
        byte[] buffer = new byte[count];
        stream.ReadExactly(buffer);
        return buffer;
    }

    internal void Skip(long count)
        => stream.Seek(count, SeekOrigin.Current);

    public void Dispose()
        => stream.Dispose();
}
