using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging.Abstractions;
using Peereflits.Shared.Cloud.Storage.Tests.Helpers;
using Xunit;

namespace Peereflits.Shared.Cloud.Storage.Tests;

[Collection(Testing.CollectionName)]
[Trait(Testing.Category, Testing.RunWithStorageEmulator)]
public class StorageContainerTests : IClassFixture<EmulatorFixture>, IAsyncLifetime
{
    private const string BlobName = "path/test.txt";

    private readonly EmulatorFixture fixture;

    private readonly StorageContainer subject;

    public StorageContainerTests(EmulatorFixture fixture)
    {
        this.fixture = fixture;

        subject = new StorageContainer(fixture.ConfigurationOne, NullLogger<StorageContainer>.Instance);
    }

    public async Task InitializeAsync()
    {
        await subject.CreateIfNotExists();
        var blob = new Blob(BlobName, Encoding.UTF8.GetBytes("Dit is een test."), "text/plain");
        var sb = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);
        await sb.Upload(blob);
    }

    public async Task DisposeAsync()
    {
        var client = new BlobServiceClient(fixture.ConfigurationOne.ConnectionString);
        BlobContainerClient container = client.GetBlobContainerClient(fixture.ConfigurationOne.ContainerName);

        await container.DeleteBlobIfExistsAsync(BlobName);
        await container.DeleteIfExistsAsync();
    }

    [Fact]
    public async Task WhenExists_WhileNot_ItShouldReturnFalse()
    {
        var container = new ContainerConfiguration
                        {
                            ConnectionString = fixture.ConfigurationOne.ConnectionString,
                            ContainerName = "notacontainer"
                        };

        var subject2 = new StorageContainer(container, NullLogger<StorageContainer>.Instance);
        bool actual = await subject2.Exists();
        Assert.False(actual);
    }

    [Fact]
    public async Task WhenExists_ItShouldReturnTrue()
    {
        bool actual = await subject.Exists();
        Assert.True(actual);
    }

    [Fact]
    public async Task WhenGetBlobNames_WithoutFilter_ItShouldReturnListOfAvailableBlobNames()
    {
        IEnumerable<string> blobs = await subject.GetBlobNames();
        Assert.Contains(blobs, x => x == BlobName);
    }

    [Fact]
    public async Task WhenGetBlobNames_WithFilter_ItShouldReturnListOfAvailableBlobNames()
    {
        IEnumerable<string> blobs = await subject.GetBlobNames("path");
        Assert.Contains(blobs, x => x == BlobName);
    }

    [Fact]
    public async Task WhenGetBlobNames_WithInvalidFilter_ItShouldReturnAnEmptyList()
    {
        IEnumerable<string> blobs = await subject.GetBlobNames("invalid/prefix");
        Assert.Empty(blobs);
    }
}