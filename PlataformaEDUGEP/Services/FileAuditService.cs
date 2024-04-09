using PlataformaEDUGEP.AuxilliaryClasses;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Services
{
    public class FileAuditService : IFileAuditService
    {
        private readonly ApplicationDbContext _context;

        public FileAuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordCreationAsync(StoredFile storedFile, string userId)
        {
            await RecordActionAsync(storedFile, userId, "Criação");
        }

        public async Task RecordDeletionAsync(StoredFile storedFile, string userId)
        {
            await RecordActionAsync(storedFile, userId, "Remoção");
        }

        public async Task RecordEditAsync(StoredFile storedFile, string userId)
        {
            await RecordActionAsync(storedFile, userId, "Edição");
        }

        private async Task RecordActionAsync(StoredFile storedFile, string userId, string actionType)
        {
            var folder = await _context.Folder.FindAsync(storedFile.FolderId);
            if (folder == null)
            {
                throw new ArgumentException("Invalid folder ID.");
            }

            // Utilize TimeZoneHelper for time conversion
            DateTime londonTime = TimeZoneHelper.ConvertUtcToLondonTime(DateTime.UtcNow);

            var audit = new FileAudit
            {
                FileId = storedFile.StoredFileId,
                UserId = userId,
                Timestamp = londonTime,
                ActionType = actionType,
                StoredFileTitle = storedFile.StoredFileTitle,
                FolderName = folder.Name
            };

            _context.FileAudits.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}