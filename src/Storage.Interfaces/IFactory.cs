namespace Peereflits.Shared.Cloud.Storage;

/// <summary>
///     A factory for cloud storage objects.
/// </summary>
/// <remarks>
///     <para>
///         The main interfaces for cloud storage are <see cref="IStorageContainer" />, which acts as a directory in the
///         cloud, and <see cref="IStorageBlob" /> which acts as a file in the cloud.
///     </para>
/// </remarks>
public interface IFactory
{
    /// <summary>
    ///     Creates an instance of a <see cref="IStorageContainer" />.
    /// </summary>
    /// <param name="storageConnectionString">The connection string of the Azure storage account.</param>
    /// <param name="containerName">The name of the storage container to use.</param>
    /// <returns>An instance of <see cref="IStorageContainer" />.</returns>
    IStorageContainer CreateStorageContainer(string storageConnectionString, string containerName);

    /// <summary>
    ///     Creates an instance of a <see cref="IStorageBlob" />.
    /// </summary>
    /// <param name="storageConnectionString">The connection string of the Azure storage account.</param>
    /// <param name="containerName">The name of the storage container to use.</param>
    /// <returns>An instance of <see cref="IStorageBlob" />.</returns>
    IStorageBlob CreateStorageBlob(string storageConnectionString, string containerName);
}