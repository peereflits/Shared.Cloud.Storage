using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Peereflits.Shared.Cloud.Storage;

public class Factory : IFactory
{
    private readonly ILoggerFactory loggerFactory;

    public static IFactory CreateWithoutLogging() => new Factory(NullLoggerFactory.Instance);

    internal Factory(ILoggerFactory loggerFactory) => this.loggerFactory = loggerFactory;

    public IStorageContainer CreateStorageContainer(string storageConnectionString, string containerName)
    {
        Guard(storageConnectionString, nameof(storageConnectionString));
        Guard(containerName, nameof(containerName));

        var config = new ContainerConfiguration
                     {
                         ConnectionString = storageConnectionString,
                         ContainerName = containerName
                     };

        return new StorageContainer(config, loggerFactory.CreateLogger<StorageContainer>());
    }

    public IStorageBlob CreateStorageBlob(string storageConnectionString, string containerName)
    {
        Guard(storageConnectionString, nameof(storageConnectionString));
        Guard(containerName, nameof(containerName));

        var config = new ContainerConfiguration
                     {
                         ConnectionString = storageConnectionString,
                         ContainerName = containerName
                     };

        return new StorageBlob(config, loggerFactory.CreateLogger<StorageBlob>());
    }

    private static void Guard(string value, string propertyName)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(propertyName);
        }
    }
}