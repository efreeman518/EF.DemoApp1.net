using Azure;
using Azure.Storage.Sas;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Storage;
using Package.Infrastructure.Test.Integration.Blob;
using System.Text;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Storage account required - Azurite storage emulator or a real Azure storage account.")]

[TestClass]
public class AzureBlobStorageTests : IntegrationTestBase
{
    private readonly IBlobRepository1 _blobRepo;
    private const string _containerNameStatic = "test-local";

    public AzureBlobStorageTests()
    {
        _blobRepo = Services.GetRequiredService<IBlobRepository1>();
    }

    [TestMethod]
    public async Task UploadAndDownload_pass()
    {
        string data = "123,Spiderman,123546789,435762985762,2000.19\n543,Batman,987654321,23457692854,199.45\n";
        using MemoryStream uploadStream = new(Encoding.UTF8.GetBytes(data));

        //azure blob container name: length:3-63; allowed:lowercase,number,-
        string containerName = $"testcontainer-{Guid.NewGuid().ToString().ToLower()}";
        string blobName = $"testblob-{Guid.NewGuid().ToString().ToLower()}";

        CancellationToken token = new CancellationTokenSource().Token;

        ContainerInfo containerInfo = new()
        {
            ContainerName = containerName,
            ContainerPublicAccessType = ContainerPublicAccessType.None,
            CreateContainerIfNotExist = true
        };

        //upload 
        await _blobRepo.UploadBlobStreamAsync(containerInfo, blobName, uploadStream, cancellationToken: token);

        //reset
        string? dataDown;

        //download
        using (Stream downloadStream = await _blobRepo.StartDownloadBlobStreamAsync(containerInfo, blobName, cancellationToken: token))
        {
            StreamReader reader = new(downloadStream);
            dataDown = await reader.ReadToEndAsync();
        }

        await _blobRepo.DeleteBlobAsync(containerInfo, blobName, token);
        await _blobRepo.DeleteContainerAsync(containerInfo, token);

        Assert.IsTrue(data == dataDown);
    }

    [TestMethod]
    public async Task SasUploadDownloadDelete()
    {
        //arrange
        string data = "123,Spiderman,123546789,435762985762,2000.19\n543,Batman,987654321,23457692854,199.45\n";
        using MemoryStream uploadStream = new(Encoding.UTF8.GetBytes(data));

        //azure blob container name: length:3-63; allowed:lowercase,number,-
        string blobName = $"testblob-{Guid.NewGuid().ToString().ToLower()}";

        ContainerInfo containerInfo = new()
        {
            ContainerName = _containerNameStatic,
            ContainerPublicAccessType = ContainerPublicAccessType.None,
            CreateContainerIfNotExist = false
        };

        //act

        //request sas upload uri
        var sasUri = await _blobRepo.GenerateBlobSasUriAsync(containerInfo, blobName, BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.IsNotNull(sasUri);
        //upload by sas upload uri
        await _blobRepo.UploadBlobStreamAsync(sasUri, uploadStream);
        //attemt download by sas upload uri - expect exception
        await Assert.ThrowsExceptionAsync<RequestFailedException>(async () =>
        {
            using Stream downloadStream = await _blobRepo.StartDownloadBlobStreamAsync(sasUri);
            StreamReader reader = new(downloadStream);
            await reader.ReadToEndAsync();
        });

        //request download sas uri
        sasUri = await _blobRepo.GenerateBlobSasUriAsync(containerInfo, blobName, BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.IsNotNull(sasUri);
        //attemt upload by sas upload uri - expect exception
        await Assert.ThrowsExceptionAsync<RequestFailedException>(async () =>
        {
            uploadStream.Position = 0;
            await _blobRepo.UploadBlobStreamAsync(sasUri, uploadStream);
        });
        //download by sas download uri
        string? dataDown;
        using Stream downloadStream = await _blobRepo.StartDownloadBlobStreamAsync(sasUri);
        StreamReader reader = new(downloadStream);
        dataDown = await reader.ReadToEndAsync();
        Assert.IsTrue(data == dataDown);

        //request delete sas uri
        sasUri = await _blobRepo.GenerateBlobSasUriAsync(containerInfo, blobName, BlobSasPermissions.Delete, DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.IsNotNull(sasUri);
        //delete by sas delete uri
        await _blobRepo.DeleteBlobAsync(sasUri);
    }
}
