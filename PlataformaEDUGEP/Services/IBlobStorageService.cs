namespace PlataformaEDUGEP.Services
{
    public interface IBlobStorageService
    {
        Task UploadFileAsync(string containerName, string fileName, Stream fileStream);
        Task<Stream> DownloadFileAsync(string containerName, string fileName);
        Task DeleteFileAsync(string containerName, string fileName);
        Task CopyFileAsync(string containerName, string sourceBlobName, string destinationBlobName);
    }
}