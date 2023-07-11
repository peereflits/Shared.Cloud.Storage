using System;

namespace Peereflits.Shared.Cloud.Storage;

public class BlobInformation
{
    public string FileName { get; init; } = null!;
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset ModifiedOn { get; init; }
    public long ContentLength { get; init; }
    public string ContentType { get; init; } = null!;
}