using System;
using System.IO;

namespace Peereflits.Shared.Cloud.Storage.Tests.Helpers;

internal class FixedStream: Stream
{
    private readonly Stream baseStream;

    public FixedStream(Stream baseStream) => this.baseStream = baseStream;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => baseStream.CanWrite;

    public override void Flush()
    {
        baseStream.Flush();
    }

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => baseStream.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => baseStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        baseStream.Write(buffer, offset, count);
    }
}