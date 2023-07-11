using System;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Peereflits.Shared.Cloud.Storage.Tests.Helpers;
using Xunit;

namespace Peereflits.Shared.Cloud.Storage.Tests;

[Collection(Testing.CollectionName)]
[Trait(Testing.Category, Testing.RunWithStorageEmulator)]
public class StorageBlobTests : IClassFixture<EmulatorFixture>
{
    private readonly EmulatorFixture fixture;
    private readonly Blob testBlob;

    private readonly StorageBlob subject;

    public StorageBlobTests(EmulatorFixture fixture)
    {
        this.fixture = fixture;

        testBlob = new Blob("path/test.txt", Encoding.UTF8.GetBytes("TestContent"), "text/plain");

        subject = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);
    }

    [Fact]
    public async Task WhenAcquireLease_ItShouldSucceed()
    {
        if(!await subject.Exists(testBlob.FileName))
        {
            await subject.Upload(testBlob);
        }

        string result = await subject.AcquireLease(testBlob.FileName);

        await subject.Delete(testBlob.FileName, result);

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task WhenAcquireLease_OnNonExistingBlob_ItShouldFail()
    {
        if(await subject.Exists(testBlob.FileName))
        {
            await subject.Delete(testBlob.FileName);
        }

        await Assert.ThrowsAsync<StorageException>(()=> subject.AcquireLease(testBlob.FileName));
    }

    [Fact]
    public async Task WhenAcquireLease_ItShouldNotAcquireAnotherLease_UntilReleased()
    {
        if (!await subject.Exists(testBlob.FileName))
        {
            await subject.Upload(testBlob);
        }

        string leaseId1 = await subject.AcquireLease(testBlob.FileName);

        var ex = await Assert.ThrowsAsync<StorageException>(() => subject.AcquireLease(testBlob.FileName));
        Assert.Equal("LeaseAlreadyPresent", (ex.InnerException as RequestFailedException).ErrorCode);

        await subject.ReleaseLease(testBlob.FileName, leaseId1);

        string leaseId2 = await subject.AcquireLease(testBlob.FileName);

        await subject.Delete(testBlob.FileName, leaseId2);

        Assert.NotEqual(leaseId1, leaseId2);
    }

    [Fact]
    public async Task WhenDelete_ItShouldSucceed()
    {
        if(await subject.Exists(testBlob.FileName))
        {
            await subject.Delete(testBlob.FileName);
        }

        bool result = await subject.Exists(testBlob.FileName);

        Assert.False(result);
    }

    [Fact]
    public async Task WhenDelete_WithLease_ItShouldSucceed()
    {
        if(!await subject.Exists(testBlob.FileName))
        {
            await subject.Upload(testBlob);
        }

        string leaseId = await subject.AcquireLease(testBlob.FileName);

        await subject.Delete(testBlob.FileName, leaseId);

        Assert.False(await subject.Exists(testBlob.FileName));
    }

    [Fact]
    public async Task WhenDelete_WhileBlobIsLeased_ItShouldThrow()
    {
        var subject2 = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);

        if(!await subject2.Exists(testBlob.FileName))
        {
            await subject2.Upload(testBlob);
        }

        string leaseId = await subject2.AcquireLease(testBlob.FileName);

        await Assert.ThrowsAsync<StorageException>(() => subject.Delete(testBlob.FileName));

        await subject2.Delete(testBlob.FileName, leaseId);
    }

    [Fact]
    public async Task WhenGetBlobInformation_ItShouldReturnBlobInformation()
    {
        if(!await subject.Exists(testBlob.FileName))
        {
            await subject.Upload(testBlob);
        }

        BlobInformation result = await subject.GetBlobInformation(testBlob.FileName);

        await subject.Delete(testBlob.FileName);

        Assert.Equal(testBlob.FileName, result.FileName);
        Assert.Equal(testBlob.Content.Length, result.ContentLength);
        Assert.Equal(testBlob.ContentType, result.ContentType);
        Assert.Equal(DateTime.Now.AddHours(-2), result.CreatedOn.DateTime, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task WhenMove_ItShouldSucceed()
    {
        var subject1 = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);

        if(!await subject1.Exists(testBlob.FileName))
        {
            await subject1.Upload(testBlob);
        }

        var subject2 = new StorageBlob(fixture.ConfigurationTwo, NullLogger<StorageBlob>.Instance);

        if(await subject2.Exists(testBlob.FileName))
        {
            await subject2.Delete(testBlob.FileName);
        }

        await subject1.Move(testBlob.FileName, fixture.ConfigurationTwo.ContainerName);

        bool result1 = await subject1.Exists(testBlob.FileName);
        bool result2 = await subject2.Exists(testBlob.FileName);

        await subject2.Delete(testBlob.FileName);

        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task WhenMove_WithLease_WhileTargetDoesNotExist_ItShouldSucceed()
    {
        var subject1 = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);

        if(!await subject1.Exists(testBlob.FileName))
        {
            await subject1.Upload(testBlob);
        }

        var subject2 = new StorageBlob(fixture.ConfigurationTwo, NullLogger<StorageBlob>.Instance);

        if(await subject2.Exists(testBlob.FileName))
        {
            await subject2.Delete(testBlob.FileName);
        }

        string lease = await subject1.AcquireLease(testBlob.FileName);

        await subject1.Move(testBlob.FileName, fixture.ConfigurationTwo.ContainerName, lease);

        bool result1 = await subject1.Exists(testBlob.FileName);
        bool result2 = await subject2.Exists(testBlob.FileName);

        await subject2.Delete(testBlob.FileName);

        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task WhenMove_WithLeases_WhileTargetExists_ItShouldOverwrite()
    {
        var subject1 = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);

        if(!await subject1.Exists(testBlob.FileName))
        {
            await subject1.Upload(testBlob);
        }

        var subject2 = new StorageBlob(fixture.ConfigurationTwo, NullLogger<StorageBlob>.Instance);

        if(!await subject2.Exists(testBlob.FileName))
        {
            await subject2.Upload(testBlob);
        }

        string lease1 = await subject1.AcquireLease(testBlob.FileName);
        string lease2 = await subject2.AcquireLease(testBlob.FileName);

        await subject1.Move(testBlob.FileName, fixture.ConfigurationTwo.ContainerName, lease1, lease2);

        bool result1 = await subject1.Exists(testBlob.FileName);
        bool result2 = await subject2.Exists(testBlob.FileName);

        await subject2.Delete(testBlob.FileName, lease2);

        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task WhenMove_WithoutLease_WhileTargetDoesNotExist_ItShouldSucceed()
    {
        var subject1 = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);

        if(!await subject1.Exists(testBlob.FileName))
        {
            await subject1.Upload(testBlob);
        }

        var subject2 = new StorageBlob(fixture.ConfigurationTwo, NullLogger<StorageBlob>.Instance);

        if(await subject2.Exists(testBlob.FileName))
        {
            await subject2.Delete(testBlob.FileName);
        }

        await subject1.Move(testBlob.FileName, fixture.ConfigurationTwo.ContainerName);

        bool result1 = await subject1.Exists(testBlob.FileName);
        bool result2 = await subject2.Exists(testBlob.FileName);

        await subject2.Delete(testBlob.FileName);

        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task WhenLeasedDownload_ItShouldSucceed()
    {
        if(!await subject.Exists(testBlob.FileName))
        {
            await subject.Upload(testBlob);
        }

        string leaseId = await subject.AcquireLease(testBlob.FileName);

        Blob result = await subject.LeasedDownload(testBlob.FileName, leaseId);

        string expected = GetContent(testBlob);
        string actual = GetContent(result);

        await subject.Delete(testBlob.FileName, leaseId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task WhenLeasedDownload_WithoutBloName_ItShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.LeasedDownload(string.Empty, "lease"));
    }

    [Fact]
    public async Task WhenLeasedDownload_WithoutLeaseId_ItShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.LeasedDownload("path1/test1.txt", string.Empty));
    }

    [Fact]
    public async Task WhenLeasedDownload_WhileBlobNotExists_ItShouldThrow()
    {
        await Assert.ThrowsAsync<StorageException>(() => subject.LeasedDownload("this/is/a/nonexisting.blob", "lease"));
    }

    [Fact]
    public async Task WhenUpload_WhileBlobDoestNotExist_ItShouldSucceed()
    {
        if(await subject.Exists(testBlob.FileName))
        {
            await subject.Delete(testBlob.FileName);
        }

        await subject.Upload(testBlob);

        bool result = await subject.Exists(testBlob.FileName);

        await subject.Delete(testBlob.FileName);

        Assert.True(result);
    }

    [Fact]
    public async Task WhenUpload_WithLease_WhileBlobExists_ItShouldOverwrite()
    {
        var original = new Blob("path2/test2.txt", Encoding.UTF8.GetBytes("TestContent"), "text/plain");
        var modified = new Blob("path2/test2.txt", Encoding.UTF8.GetBytes("OtherContent"), "text/plain");

        if(!await subject.Exists(original.FileName))
        {
            await subject.Upload(original);
        }

        string lease = await subject.AcquireLease(original.FileName);

        await subject.Upload(modified, lease);

        Blob result = await subject.LeasedDownload(modified.FileName, lease);

        string expected = GetContent(modified);
        string actual = GetContent(result);

        await subject.Delete(modified.FileName, lease);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task WhenUpload_WithoutLease_WhileBlobExists_ItShouldOverwrite()
    {
        var original = new Blob("path2/test2.txt", Encoding.UTF8.GetBytes("TestContent"), "text/plain");
        var modified = new Blob("path2/test2.txt", Encoding.UTF8.GetBytes("OtherContent"), "text/plain");

        if(!await subject.Exists(original.FileName))
        {
            await subject.Upload(original);
        }

        await subject.Upload(modified);

        Blob result = await subject.Download(modified.FileName);

        string expected = GetContent(modified);
        string actual = GetContent(result);

        await subject.Delete(modified.FileName);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task WhenUpload_WhileTargetIsAlreadyLeased_ItShouldThrow()
    {
        var target = new StorageBlob(fixture.ConfigurationOne, NullLogger<StorageBlob>.Instance);

        if(!await target.Exists(testBlob.FileName))
        {
            await target.Upload(testBlob);
        }
        string lease = await target.AcquireLease(testBlob.FileName);

        var ex = await Assert.ThrowsAsync<StorageException>(()=> subject.Upload(testBlob));
        Assert.Equal("LeaseIdMissing", (ex.InnerException as RequestFailedException).ErrorCode);

        await target.Delete(testBlob.FileName, lease);
    }

    private static string GetContent(Blob blob) => Encoding.UTF8.GetString(blob.Content);
}