using System;

namespace Peereflits.Shared.Cloud.Storage;

public class StorageException : Exception
{
    public StorageException() { }

    public StorageException(string message) : base(message) { }

    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}