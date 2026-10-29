namespace Export.Core.Exports;

// .NET 10 zip entries dispose synchronously (dotnet/runtime#121624, fixed in .NET 11); Kestrel rejects sync writes,
// so sync writes are held here and sent asynchronously before the next write or on DrainAsync.
public sealed class SyncBufferingStream(Stream inner) : Stream
{
    private readonly MemoryStream pending = new();

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(byte[] buffer, int offset, int count) => pending.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => pending.Write(buffer);

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await DrainAsync(cancellationToken);
        await inner.WriteAsync(buffer, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public async Task DrainAsync(CancellationToken cancellationToken)
    {
        if (pending.Length == 0)
        {
            return;
        }

        await inner.WriteAsync(pending.GetBuffer().AsMemory(0, (int)pending.Length), cancellationToken);
        pending.SetLength(0);
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => DrainAsync(cancellationToken);

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pending.Dispose();
        }

        base.Dispose(disposing);
    }
}