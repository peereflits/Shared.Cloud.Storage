![Logo](./img/peereflits-logo.svg) 

# Peereflits.Shared.Cloud.Storage

`Peereflits.Shared.Cloud.Storage` is a wrapper library on top of the [Azure.Storage.Blobs SDK](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage?view=azure-dotnet) that allows [Blobs](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction#blobs) and [Containers](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction#containers) are manageable (read, add, delete).

The purpose of *Peereflits.Shared.Cloud.Storage* is:
1. Simplicity in use
1. Handling transient errors
1. Logging interaction with Blobs and Containers


## Storage packages, dependencies & class diagram

`Peereflits.Shared.Cloud.Storage` consists of two packages:
1. `Peereflits.Shared.Cloud.Storage.Interfaces`
    * Dependencies: none
    * contains interfaces, DTOs, exceptions
1. "Peereflits.Shared.Cloud.Storage".
    * Implements `Storage.Interfaces`
    * Has dependencies on:
       * `Azure.Storage.Blobs` for interacting with Blobs and Containers

See the class diagram below for an overview of the most important classes and interfaces.

<!-- Klik op het diagram om deze te openen in https://mermaid.live/ -->
[![](https://mermaid.ink/img/pako:eNqlVF9v2yAQ_yqIp1RL-wGiLNPWtJOlppWW7GnZA7bPLhqGDM5bo6z57AOMHRLb68OejO9-f47j4EAzlQOd0UwwY5aclZpVW-n_SLJGpVkJt0oi4xL0YSsJmc-5RNAFy2CxcIF3BjWXJXlkFfj_uxdu0EyuyIaZH8dUKXH08VsNDCEpHhWeQXzyM-AnoVInYiaN4gey01DwF_KeyFqIoJfcyboCzVIBxwZ3tPKvl0U7sUNcTqgyDSa96r7uhGL5xPE8aEraKgQwA0l-VoanLNVv6UlvaT84hXwM3jq1Rj36EgQgjNH-UeBK_RqlEWS6BOwO91zSqFpn4OsOsqdcQ4xzkeXH7GfNdcMc6Us4NQ__Ar74QfhwX-JpSWShdMWQKznidYGKJ-U0IYF6zwV0Q5zuEb59J649IDGGhdBmv4MLtchoXHhp78CGV_BUFAaQNJcif5JDyZXKecG7rFAn-weQJT6_XVdyzzJ7I_Yjd7exv7zobTNNF5eQuW2tfbw7mCwenqv-g9G3cG36T3Un4TfYs5v_ub7ulxDzIkQj07bHJcLynHFzswjIfjA675OSzQ30YSDbyNIpta9ZxXhuX2F_SFuKz2CHhc7sMoeC1QK31O7YQlmNar2XGZ2hrmFK611uexvebTormDA2Cjm3FqvwsrvP61_ymCWS?type=png)](https://mermaid.live/edit#pako:eNqlVF9v2yAQ_yqIp1RL-wGiLNPWtJOlppWW7GnZA7bPLhqGDM5bo6z57AOMHRLb68OejO9-f47j4EAzlQOd0UwwY5aclZpVW-n_SLJGpVkJt0oi4xL0YSsJmc-5RNAFy2CxcIF3BjWXJXlkFfj_uxdu0EyuyIaZH8dUKXH08VsNDCEpHhWeQXzyM-AnoVInYiaN4gey01DwF_KeyFqIoJfcyboCzVIBxwZ3tPKvl0U7sUNcTqgyDSa96r7uhGL5xPE8aEraKgQwA0l-VoanLNVv6UlvaT84hXwM3jq1Rj36EgQgjNH-UeBK_RqlEWS6BOwO91zSqFpn4OsOsqdcQ4xzkeXH7GfNdcMc6Us4NQ__Ar74QfhwX-JpSWShdMWQKznidYGKJ-U0IYF6zwV0Q5zuEb59J649IDGGhdBmv4MLtchoXHhp78CGV_BUFAaQNJcif5JDyZXKecG7rFAn-weQJT6_XVdyzzJ7I_Yjd7exv7zobTNNF5eQuW2tfbw7mCwenqv-g9G3cG36T3Un4TfYs5v_ub7ulxDzIkQj07bHJcLynHFzswjIfjA675OSzQ30YSDbyNIpta9ZxXhuX2F_SFuKz2CHhc7sMoeC1QK31O7YQlmNar2XGZ2hrmFK611uexvebTormDA2Cjm3FqvwsrvP61_ymCWS)


## Usage

The starting point of using `Peereflits.Shared.Cloud.Storage` is `IStorageFactory`. Instances of `IStorageContainer` and `IStorageBlob` can be obtained through this interface. For unit-testing purposes there is also a `static IFactory CreateWithoutLogging()` available.

Logging and transient error handling is standard built in (except for the `CreateWithoutLogging`).


## Example

Below is an implementation example.

``` csharp
internal class StorageBlobRetriever : IRetrieveStorageBlob
{
    private const string AppContainerName = "my-app-container";
    
    private readonly IStorageFactory factory;
    private readonly AppSettings settings;
    private IStorageBlob storageBlob;
    private bool isInitialized;
    
    public StorageBlobsProvider
    (
        IStorageFactory factory, 
        AppSettings settings
    )
    { 
        this.factory = factory;
        this.settings = settings;
    }
    
    public async Task<bool> Exists(string blobName)
    {
        await Initialize();
        return await storageBlob.Exists(blobName);
    }
    
    private async Task Initialize()
    {
        if(isInitialized) return;
        
        var container = factory.CreateStorageContainer  (settings.StorageConnectionString,   AppContainerName);
        await container.CreateIfNotExists();
        storageBlob = factory.CreateStorageBlob(settings.StorageConnectionString,     AppContainerName);
        isInitialized = true;
    }
    
    public async Task<byte[]> GetContent(string blobName)
    {
        if(await !Exists(blobName)) return Array.Empty<byte>();
        
        var blob = await storageBlob.Download(blobName);
        return blob.Content;
    }
    
    public async Task<BlobInfo> GetInfo(string blobName)
    {
        if(await !Exists(blobName)) return new BlobInfo{ FileName = blobName; };
        
        var info = await storageBlob.GetBlobInformation(blobName);
        return new BlobInfo
               {
                 FileName = info.FileName;
                 ContentLength = info.ContentLength;
                 ContentType = info.ContentType;
               };
    }
}

public record BlobInfo
{
  public string FileName { get; init; } = null!;
  public long ContentLength { get; init; } = string.Empty;
  public string ContentType { get; init; } = string.Empty;
}
```

### Version support

The libraries supports the following .NET versions:
1. .NET 6.0
1. .NET 7.0
1. .NET 8.0

---

<p align="center">
&copy; No copyright applicable<br />
&#174; "Peereflits" is my codename.
</p>

---
