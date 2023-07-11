using System.IO;

namespace Peereflits.Shared.Cloud.Storage;

public class Blob
{
    public Blob(string fileName, byte[] content, string contentType)
    {
        FileName = fileName;
        Content = content;
        ContentType = contentType;
    }

    public Blob(string fileName, Stream content, string contentType) : this(fileName, ToBytes(content), contentType) { }

    public string FileName { get; }
    public byte[] Content { get; }
    public string ContentType { get; }

    public Stream Stream => new MemoryStream(Content);

    private static byte[] ToBytes(Stream stream)
    {
        if(!stream.CanRead)
        {
            throw new StorageException("A blob can't be created from an unreadable stream.");
        }

        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        mem.Position = 0;
        return mem.ToArray();
    }
}