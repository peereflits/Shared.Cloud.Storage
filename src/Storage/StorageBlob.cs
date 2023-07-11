using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

namespace Peereflits.Shared.Cloud.Storage;

internal class StorageBlob : StorageBase, IStorageBlob
{
    public StorageBlob
    (
        ContainerConfiguration configuration,
        ILogger<StorageBlob> logger
    ) : base(configuration, logger) { }

    public async Task<bool> Exists(string blobName)
    {
        LogStart(nameof(Exists), blobName);
        Guard(blobName);

        try
        {
            bool result = await BlobExists(blobName);

            LogFinish(nameof(Exists), blobName, result);
            return result;
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to determine whether blob {blobName} exists.", blobName);
        }
    }

    private async Task<bool> BlobExists(string name)
    {
            Response<bool>? result = await GetBlobClient(name)
                                          .ExistsAsync()
                                          .ConfigureAwait(false);
            return result.Value;
    }



    public async Task<BlobInformation> GetBlobInformation(string blobName)
    {
        LogStart(nameof(GetBlobInformation), blobName);
        Guard(blobName);

        if(!await BlobExists(blobName))
        {
            throw new StorageException($"Blob {blobName} does not exists.");
        }

        try
        {
            Response<BlobProperties>? response = await GetBlobClient(blobName)
                                                      .GetPropertiesAsync()
                                                      .ConfigureAwait(false);

            var result = new BlobInformation
                         {
                             FileName = blobName,
                             CreatedOn = response.Value.CreatedOn,
                             ModifiedOn = response.Value.LastModified,
                             ContentLength = response.Value.ContentLength,
                             ContentType = response.Value.ContentType
                         };

            LogFinish(nameof(GetBlobInformation), blobName);
            return result;
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to retrieve blob information for {blobName}.", blobName);
        }
    }

    public async Task Upload(Blob blob, string? leaseId = null)
    {
        LogStart(nameof(Upload), blob.FileName, leaseId ?? string.Empty);

        if(blob == null)
        {
            throw new ArgumentNullException(nameof(blob));
        }

        if(string.IsNullOrEmpty(blob.FileName))
        {
            throw new ArgumentException("Filename of blob is missing.");
        }

        if(!blob.Content.Any())
        {
            throw new ArgumentException("Blob has no content.");
        }

        if(string.IsNullOrEmpty(blob.ContentType))
        {
            throw new ArgumentException("Content type of blob is missing.");
        }

        try
        {
            if(blob.Stream.CanSeek)
            {
                blob.Stream.Position = 0;
            }

            await GetBlobClient(blob.FileName)
                 .UploadAsync(blob.Stream,
                              new BlobHttpHeaders { ContentType = blob.ContentType },
                              conditions: new BlobRequestConditions { LeaseId = leaseId }
                             )
                 .ConfigureAwait(false);

            LogFinish(nameof(Upload), blob.FileName, leaseId ?? string.Empty);
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to upload blob {blob.FileName}.", blob.FileName);
        }
    }

    public async Task<Blob> Download(string blobName)
    {
        LogStart(nameof(Download), blobName);
        Guard(blobName);

        if(!await BlobExists(blobName))
        {
            throw new StorageException($"Blob {blobName} does not exists.");
        }

        try
        {
            Response<BlobDownloadInfo> blobDownloadInfo = await GetBlobClient(blobName)
                                                               .DownloadAsync()
                                                               .ConfigureAwait(false);

            var result = new Blob(blobName, blobDownloadInfo.Value.Content, blobDownloadInfo.Value.ContentType);

            LogFinish(blobName);

            return result;
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to download blob {blobName}.", blobName);
        }
    }

    public async Task<Blob> LeasedDownload(string blobName, string leaseId)
    {
        LogStart(nameof(LeasedDownload), blobName, leaseId);

        Guard(blobName);
        Guard(leaseId, nameof(leaseId));

        if(!await BlobExists(blobName))
        {
            throw new StorageException($"Blob {blobName} does not exists.");
        }

        try
        {
            Response<BlobDownloadInfo> blobDownloadInfo = await GetBlobClient(blobName)
                                                               .DownloadAsync(conditions: new BlobRequestConditions { LeaseId = leaseId })
                                                               .ConfigureAwait(false);

            var result = new Blob(blobName, blobDownloadInfo.Value.Content, blobDownloadInfo.Value.ContentType);

            LogFinish(nameof(LeasedDownload), blobName, leaseId);

            return result;
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to determine whether blob {blobName} exists.", blobName);
        }
    }

    public async Task Delete(string blobName, string? leaseId = null)
    {
        LogStart(nameof(Delete), blobName, leaseId ?? string.Empty);
        Guard(blobName);

        try
        {
            await GetBlobClient(blobName)
                 .DeleteIfExistsAsync(conditions: new BlobRequestConditions { LeaseId = leaseId })
                 .ConfigureAwait(false);

            LogFinish(nameof(Delete), blobName, leaseId ?? string.Empty);
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to delete blob {blobName}.", blobName);
        }
    }

    public async Task Move
    (
        string blobName,
        string targetContainerName,
        string? sourceLease = null,
        string? targetLease = null
    )
    {
        Logger.LogInformation($"Executing {nameof(Move)} of {{BlobName}} from {{ContainerName}} to {{TargetContainer}}",
                              blobName,
                              Configuration.ContainerName,
                              targetContainerName
                             );

        Guard(blobName);
        Guard(targetContainerName, nameof(targetContainerName));

        if(!await BlobExists(blobName))
        {
            throw new StorageException($"Blob {blobName} does not exists.");
        }

        var targetConfig = new ContainerConfiguration
                           {
                               ConnectionString = Configuration.ConnectionString,
                               ContainerName = targetContainerName
                           };

        if(!await GetContainerClient(targetConfig).ExistsAsync())
        {
            throw new StorageException($"The target container {targetContainerName} does not exists.");
        }

        BlobClient sourceClient = GetBlobClient(blobName);
        BlobClient targetClient = GetBlobClient(targetContainerName, blobName);

        try
        {
            Response<BlobDownloadInfo> info = await sourceClient.DownloadAsync(conditions: new BlobRequestConditions { LeaseId = sourceLease });

            await targetClient.UploadAsync(info.Value.Content,
                                           new BlobHttpHeaders { ContentType = info.Value.ContentType },
                                           conditions: new BlobRequestConditions { LeaseId = targetLease }
                                          );

            await sourceClient.DeleteIfExistsAsync(conditions: new BlobRequestConditions { LeaseId = sourceLease });

            Logger.LogInformation($"Executed {nameof(Move)} of {{BlobName}} from {{ContainerName}} to {{TargetContainer}}",
                                  blobName,
                                  Configuration.ContainerName,
                                  targetContainerName
                                 );
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to move blob {blobName} from {Configuration.ContainerName} to {targetContainerName}.", blobName);
        }
    }

    public async Task<string> AcquireLease(string blobName)
    {
        LogStart(nameof(AcquireLease), blobName);
        Guard(blobName);

        if (!await BlobExists(blobName))
        {
            throw new StorageException($"Cannot Acquire Lease for blob {blobName} as it doesn't exist.");
        }

        try
        {
            BlobLeaseClient client = GetBlobClient(blobName).GetBlobLeaseClient();

            Response<BlobLease> lease = await client.AcquireAsync(TimeSpan.FromSeconds(60))
                                                    .ConfigureAwait(false);

            string result = lease?.Value.LeaseId ?? string.Empty;

            LogFinish(nameof(AcquireLease), blobName);

            return result;
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to acquire lease for blob {blobName}.", blobName);
        }
    }

    public async Task ReleaseLease(string blobName, string leaseId)
    {
        LogStart(nameof(ReleaseLease), blobName, leaseId);
        Guard(blobName);
        Guard(leaseId, nameof(leaseId));

        if(!await BlobExists(blobName))
        {
            throw new StorageException($"Blob {blobName} does not exists.");
        }

        try
        {
            BlobLeaseClient client = GetBlobClient(blobName).GetBlobLeaseClient(leaseId);

            await client.ReleaseAsync()
                        .ConfigureAwait(false);

            LogFinish(nameof(ReleaseLease), blobName, leaseId);
        }
        catch(Exception e)
        {
            throw BuildCloudException(e, $"Failed to release lease {leaseId} for blob {blobName}.", blobName);
        }
    }

    private BlobClient GetBlobClient(string fileName) => Container.GetBlobClient(fileName);

    /// <summary>
    ///     Gets a <see cref="BlobClient" /> for a storage <paramref name="container" /> on the same storage account this
    ///     instance was created for.
    /// </summary>
    /// <param name="container">The storage container the client should use.</param>
    /// <param name="blobName">The name of the blob to create the client for.</param>
    /// <returns>An instance of a <see cref="BlobClient" />.</returns>
    private BlobClient GetBlobClient(string container, string blobName)
    {
        var targetConfig = new ContainerConfiguration
                           {
                               ConnectionString = Configuration.ConnectionString,
                               ContainerName = container
                           };

        return GetContainerClient(targetConfig).GetBlobClient(blobName);
    }

    private static void Guard(string value) => Guard(value, "blobName");

    private static void Guard(string value, string propertyName)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(propertyName);
        }
    }
}