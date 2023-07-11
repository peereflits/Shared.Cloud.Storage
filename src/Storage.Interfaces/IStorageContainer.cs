using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peereflits.Shared.Cloud.Storage;

public interface IStorageContainer
{
    string Name { get; }
    Task<bool> Exists();
    Task CreateIfNotExists();
    Task<IEnumerable<string>> GetBlobNames(string? prefix = null);
}