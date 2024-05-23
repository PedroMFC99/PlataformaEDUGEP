using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Threading.Tasks;

namespace PlataformaEDUGEP.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task UploadFileAsync(string containerName, string fileName, Stream fileStream)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);
        }

        public async Task<Stream> DownloadFileAsync(string containerName, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            var downloadInfo = await blobClient.DownloadAsync();
            return downloadInfo.Value.Content;
        }

        public async Task DeleteFileAsync(string containerName, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task CopyFileAsync(string containerName, string sourceBlobName, string destinationBlobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var sourceBlobClient = containerClient.GetBlobClient(sourceBlobName);
            var destinationBlobClient = containerClient.GetBlobClient(destinationBlobName);

            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

            // Optionally, you can wait for the copy to complete if necessary
            BlobProperties properties = await destinationBlobClient.GetPropertiesAsync();
            while (properties.CopyStatus == CopyStatus.Pending)
            {
                await Task.Delay(100);
                properties = await destinationBlobClient.GetPropertiesAsync();
            }

            if (properties.CopyStatus != CopyStatus.Success)
            {
                throw new Exception($"Failed to copy blob: {properties.CopyStatusDescription}");
            }
        }
    }
}