using System.Buffers;

namespace MemoryMapped.Queue;
public sealed class PooledBufferWriter(int initialCapacity = 16 * 1024) : IBufferWriter<byte>, IDisposable
{
    private byte[] buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    private int written;
    private bool disposed;

    public void Advance(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (written + count > buffer.Length) throw new InvalidOperationException("Cannot advance past buffer size");
        written += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        CheckDisposed();
        Ensure(sizeHint);
        return buffer.AsMemory(written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        CheckDisposed();
        Ensure(sizeHint);
        return buffer.AsSpan(written);
    }

    private void Ensure(int sizeHint)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        var availableSpace = buffer.Length - written;
        if (availableSpace >= sizeHint) return;

        var requiredSize = written + sizeHint;
        var newSize = Math.Max(buffer.Length * 2, requiredSize);

        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        buffer.AsSpan(0, written).CopyTo(newBuffer);

        ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
        buffer = newBuffer;
    }

    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            CheckDisposed();
            return new ReadOnlySpan<byte>(buffer, 0, written);
        }
    }

    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            CheckDisposed();
            return new ReadOnlyMemory<byte>(buffer, 0, written);
        }
    }

    public int WrittenCount => written;

    public int Capacity => buffer.Length;

    // Reuse the writer without reallocating
    public void Clear()
    {
        CheckDisposed();
        written = 0;
    }

    private void CheckDisposed()
    {
        if (disposed) throw new ObjectDisposedException(nameof(PooledBufferWriter));
    }

    public void Dispose()
    {
        if (disposed) return;

        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
        }

        buffer = [];
        written = 0;
        disposed = true;
    }
}