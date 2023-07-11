# Run unittests with Azurite Azure Storage Emulator #

You can run the unit tests that are marked as "*RunWithStorageEmulator**" localy if you have Azurite (a Azure Storage Emulator) installed.
Azurite is default installed with Visual Studio 2022.

*Azurite* is started in the `EmulatorFixture` in the context of VS2022.

For more info, see [Use the Azurite emulator for local Azure Storage development](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio)

**Pro tip:** if a blob- or container test fails it migth happen that Azurite is still running in the background with a lock (called "*lease*") on a blob. Cancelling the tests an rerunning them migth cause failing the tests again. This is a false negative.

To solve this:

1. In the task manager kill Azurite.
1. Drop folder `$\\src\Storage.Tests\bin\Debug\net6.0\__blobstorage__`
