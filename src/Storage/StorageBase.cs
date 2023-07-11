using System;
using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Peereflits.Shared.Cloud.Storage;

internal abstract class StorageBase
{
    private BlobContainerClient? containerClient;

    protected StorageBase
    (
        ContainerConfiguration configuration,
        ILogger<StorageBase> logger    )
    {
        Configuration = configuration;
        Logger = logger;
    }


    protected ContainerConfiguration Configuration { get; }
    protected ILogger<StorageBase> Logger { get; }

    protected BlobContainerClient Container => containerClient ??= GetContainerClient(Configuration);

    internal static BlobContainerClient GetContainerClient(ContainerConfiguration configuration)
    {
        var options = new BlobClientOptions { Retry = { MaxRetries = 3 } };
        var client = new BlobServiceClient(configuration.ConnectionString, options);

        return client.GetBlobContainerClient(configuration.ContainerName);
    }

    protected void LogStart(string caller)
    {
        Logger.LogInformation($"Executing {caller} on {{ContainerName}}", Configuration.ContainerName);
    }

    protected void LogStart(string caller, string blobName)
    {
        Logger.LogInformation($"Executing {caller} for {{BlobName}} on {{ContainerName}}", blobName, Configuration.ContainerName);
    }

    protected void LogStart(string caller, string blobName, string leaseId)
    {
        Logger.LogInformation($"Executing {caller} for {{BlobName}} with lease {{Lease}} on {{ContainerName}}",
                              blobName,
                              leaseId,
                              Configuration.ContainerName
                             );
    }

    protected void LogFinish(string caller)
    {
        Logger.LogInformation($"Executed {caller} on {{ContainerName}}", Configuration.ContainerName);
    }
    protected void LogFinish(string caller, string blobName)
    {
        Logger.LogInformation($"Executed {caller} for {{BlobName}} on {{ContainerName}}", blobName, Configuration.ContainerName);
    }

    protected void LogFinish(string caller, string blobName, string leaseId)
    {
        Logger.LogInformation($"Executed {caller} for {{BlobName}} with lease {{Lease}} on {{ContainerName}}",
                              blobName,
                              leaseId,
                              Configuration.ContainerName
                             );
    }

    protected void LogFinish(string caller, string blobName, object result)
    {
        Logger.LogInformation($"Executed {caller} for {{BlobName}} on {{ContainerName}} resulted in: {{Result}}",
                              blobName,
                              Configuration.ContainerName,
                              result
                             );
    }

    protected void LogFinish(string caller, string blobName, string leaseId, object result)
    {
        Logger.LogInformation($"Executed {caller} for {{BlobName}} with lease {{Lease}} on {{ContainerName}} resulted in: {{Result}}",
                              blobName,
                              leaseId,
                              Configuration.ContainerName,
                              result
                             );
    }

    protected StorageException BuildCloudException(Exception innerEx, string errorMessage, string blobName, [CallerMemberName] string? caller = null)
    {
        var result = new StorageException(errorMessage, innerEx)
                     {
                         Data = { ["ContainerConnectionString"] = Configuration.ConnectionString }
                     };

        Logger.LogError(innerEx,
                        $"Failed to execute {caller} for {{BlobName}} on {{ContainerName}}",
                        blobName,
                        Configuration.ContainerName
                       );

        return result;
    }}