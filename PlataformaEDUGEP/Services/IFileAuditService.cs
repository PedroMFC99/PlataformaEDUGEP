using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Services
{
    public interface IFileAuditService
    {
        Task RecordCreationAsync(StoredFile storedFile, string userId);
        Task RecordDeletionAsync(StoredFile storedFile, string userId);
        Task RecordEditAsync(StoredFile storedFile, string userId);
    }
}
