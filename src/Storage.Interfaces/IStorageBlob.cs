using System.Threading.Tasks;

namespace Peereflits.Shared.Cloud.Storage;

public interface IStorageBlob
{
    Task<bool> Exists(string blobName);
    Task Upload(Blob blob, string? leaseId = null);
    Task<Blob> Download(string blobName);
    Task<Blob> LeasedDownload(string blobName, string leaseId);
    Task Delete(string blobName, string? leaseId = null);
    Task Move(string blobName, string targetContainerName, string? sourceLease = null, string? targetLease = null);
    Task<string> AcquireLease(string blobName);
    Task ReleaseLease(string blobName, string leaseId);
    Task<BlobInformation> GetBlobInformation(string blobName);
}