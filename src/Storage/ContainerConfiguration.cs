namespace Peereflits.Shared.Cloud.Storage;

internal class ContainerConfiguration 
{
    public string ConnectionString { get; init; } = null!;
    public string ContainerName { get; init; } = null!;
}