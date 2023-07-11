using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Xunit;

namespace Peereflits.Shared.Cloud.Storage.Tests.Helpers;

/// <see cref="https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio" />
public class EmulatorFixture : IAsyncLifetime
{
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private const string ContainerName1 = "test-container-one";
    private const string ContainerName2 = "test-container-two";
    
    private readonly Process process;

    public EmulatorFixture()
    {
        var info = new ProcessStartInfo
                   {
                       FileName = GetAzuritePath(),
                       CreateNoWindow = true,
                       UseShellExecute = false,
                       RedirectStandardOutput = true,
                       Arguments = "--skipApiVersionCheck"
                   };

        process = new Process { StartInfo = info };
        process.OutputDataReceived += (sender, data) =>
                                      {
                                          Console.WriteLine(data.Data);
                                      };

        process.Start();

        ConfigurationOne = new ContainerConfiguration
                           {
                               ConnectionString = ConnectionString,
                               ContainerName = ContainerName1
                           };

        ConfigurationTwo = new ContainerConfiguration
                           {
                               ConnectionString = ConnectionString,
                               ContainerName = ContainerName2
                           };
    }

    private string GetAzuritePath()
    {
        string programFiles = Environment.GetEnvironmentVariable("ProgramFiles");

        string[] editions = { "Professional", "Enterprise" };
        var path = $"{programFiles}\\Microsoft Visual Studio\\2022\\{editions[0]}" 
                 + "\\Common7\\IDE\\Extensions\\Microsoft\\Azure Storage Emulator\\azurite.exe";

        if(File.Exists(path))
        {
            return path;
        }

        path = $"{programFiles}\\Microsoft Visual Studio\\2022\\{editions[1]}" 
             + "\\Common7\\IDE\\Extensions\\Microsoft\\Azure Storage Emulator\\azurite.exe";
        if(File.Exists(path))
        {
            return path;
        }

        throw new NotSupportedException("You cannot run the unit test with the storage emulator because Azurite is not installed.");
    }

    internal ContainerConfiguration ConfigurationOne { get; }
    internal ContainerConfiguration ConfigurationTwo { get; }

    public async Task InitializeAsync()
    {
        var factory = Factory.CreateWithoutLogging();
        var sc1 = factory.CreateStorageContainer(ConnectionString, ContainerName1);
        await sc1.CreateIfNotExists();

        var sc2 = factory.CreateStorageContainer(ConnectionString, ContainerName2);
        await sc2.CreateIfNotExists();
    }

    public async Task DisposeAsync()
    {
        var client = new BlobContainerClient(ConnectionString, ContainerName1);
        await client.DeleteIfExistsAsync();
        client = new BlobContainerClient(ConnectionString, ContainerName2);
        await client.DeleteIfExistsAsync();
        Dispose();
    }

    private void Dispose()
    {
        if(process == null)
        {
            return;
        }

        if(!process.HasExited)
        {
            process.Kill();
        }

        process.Dispose();
    }
}