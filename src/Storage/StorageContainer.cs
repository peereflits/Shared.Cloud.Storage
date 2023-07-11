using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Peereflits.Shared.Cloud.Storage;

internal class StorageContainer : StorageBase, IStorageContainer
{
    public StorageContainer
    (
        ContainerConfiguration configuration,
        ILogger<StorageContainer> logger
    ) : base(configuration, logger)
    {
    }

    public string Name => Configuration.ContainerName;

    public async Task<bool> Exists()
    {
        LogStart(nameof(Exists));

        try
        {
            Response<bool>? result = await Container.ExistsAsync().ConfigureAwait(false);

            Logger.LogInformation($"Executed {nameof(Exists)} on {{ContainerName}}: {{Result}}", Name, result.Value);

            return result.Value;
        }
        catch(Exception e)
        {
            e.Data["ContainerConnectionString"] = Configuration.ConnectionString;
            Logger.LogError(e, $"Failed to execute {nameof(Exists)} on {{ContainerName}}", Name);
            throw new StorageException($"Failed to check for existence of storage container {Name}.", e);
        }
    }

    public async Task CreateIfNotExists()
    {
        Logger.LogInformation($"Executing {nameof(CreateIfNotExists)} for {{ContainerName}}", Name);

        try
        {
            await Container.CreateIfNotExistsAsync()
                           .ConfigureAwait(false);

            Logger.LogInformation($"Executed {nameof(CreateIfNotExists)} for {{ContainerName}}", Name);
        }
        catch(Exception e)
        {
            e.Data["ContainerConnectionString"] = Configuration.ConnectionString;
            Logger.LogError(e, $"Failed to execute {nameof(CreateIfNotExists)} for {{ContainerName}}", Name);
            throw new StorageException($"Failed to create storage container {Name}.", e);
        }
    }

    public async Task<IEnumerable<string>> GetBlobNames(string? prefix = null)
    {
        Logger.LogInformation($"Executing {nameof(GetBlobNames)} with prefix {{Prefix}} on {{ContainerName}}", prefix, Name);

        try
        {
            var result = new List<string>();

            ConfiguredCancelableAsyncEnumerable<BlobItem> blobItems = Container.GetBlobsAsync(prefix: prefix)
                                                                               .ConfigureAwait(false);

            await foreach(BlobItem blobItem in blobItems)
            {
                result.Add(blobItem.Name);
            }

            Logger.LogInformation($"Executed {nameof(GetBlobNames)} with prefix {{Prefix}} on {{ContainerName}}: {{Count}}", prefix, Name, result.Count);

            return result;
        }
        catch(Exception e)
        {
            e.Data["ContainerConnectionString"] = Configuration.ConnectionString;
            Logger.LogError(e, $"Failed to execute {nameof(GetBlobNames)} with prefix {{Prefix}} on {{ContainerName}}", prefix, Name);
            throw new StorageException($"Failed to get blob names with prefix {prefix} on {Name}", e);
        }
    }
}