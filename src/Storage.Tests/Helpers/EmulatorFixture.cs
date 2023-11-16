using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        var processes = Process.GetProcessesByName("azurite");

        if(processes.Any())
        {
            process = processes.OrderBy(x=>x.Id).First();
            return;
        }

        var info = new ProcessStartInfo
                   {
                       FileName = GetAzuritePath(),
                       CreateNoWindow = false,
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
    }

    private static string GetAzuritePath()
    {
        string programFiles = Environment.GetEnvironmentVariable("ProgramFiles") ?? "C:\\Program Files";

        string[] editions = { "Enterprise", "Professional", "Community" };

        foreach(string edition in editions)
        {
            var path = $"{programFiles}\\Microsoft Visual Studio\\2022\\{edition}" 
                     + "\\Common7\\IDE\\Extensions\\Microsoft\\Azure Storage Emulator\\azurite.exe";

            if(File.Exists(path))
            {
                return path;
            }
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
        if(!process.HasExited)
        {
            process.Kill();
        }

        process.Dispose();
    }
}