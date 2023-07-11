using System.IO;
using System.Text;
using Peereflits.Shared.Cloud.Storage.Tests.Helpers;
using Xunit;

namespace Peereflits.Shared.Cloud.Storage.Tests;

public class BlobTest
{
    [Fact]
    public void WhenConstructBlob_ItShouldHaveTheSameContentAndStream()
    {
        byte[] expected = Encoding.UTF8.GetBytes("TestContent");

        var blob = new Blob("file.txt", expected, "test/plain");
            
        byte[] a1 = blob.Content;
        using var stream = blob.Stream;

        for(var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], a1[i]);
            byte actual = (byte)stream.ReadByte();
            Assert.Equal(expected[i], actual);
        }
    }

    [Fact]
    public void WhenConstructSemanticEqualBlobs_ItShouldBeTheSame()
    {
        byte[] expected = Encoding.UTF8.GetBytes("TestContent");
        var blob1 = new Blob("file.txt", expected, "test/plain");

        using var stream = new MemoryStream(expected);
        var blob2 = new Blob("file.txt", stream, "test/plain");
            
        byte[] a1 = blob1.Content;
        byte[] a2 = blob2.Content;

        for(var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], a1[i]);
            Assert.Equal(expected[i], a2[i]);
        }
    }

    [Fact]
    public void WhenUseBlobInstance_ItsContentShouldBeReadableMultipleTimes()
    {
        byte[] expected = Encoding.UTF8.GetBytes("TestContent");

        using var stream = new MemoryStream(expected);
        var blob = new Blob("file.txt", stream, "test/plain");
            
        byte[] a1 = blob.Content;

        for(var i = 0; i < expected.Length; i++)
        {
            byte actual = blob.Content[i];
            Assert.Equal(expected[i], a1[i]);
            Assert.Equal(expected[i], actual);
        }
    }

    [Fact]
    public void WhenConstructBlobWithUnreadableStream_ItShouldThrowWhenReadContent()
    {
        byte[] expected = Encoding.UTF8.GetBytes("TestContent");

        using Stream stream = new FixedStream(new MemoryStream(expected));
            
        Assert.Throws<StorageException>(()=> new Blob("file.txt", stream, "test/plain"));
    }
}