namespace PlataformaEDUGEP.Services
{
    public interface IFolderAuditService
    {
        Task LogAuditAsync(string userId, string actionType, int folderId, string folderName);
    }
}
