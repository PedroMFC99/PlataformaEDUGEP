using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System;
using System.Threading.Tasks;

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
            var folder = await _context.Folder.FindAsync(storedFile.FolderId);
            if (folder == null)
            {
                throw new ArgumentException("Invalid folder ID.");
            }

            var audit = new FileAudit
            {
                FileId = storedFile.StoredFileId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ActionType = "Criação",
                StoredFileTitle = storedFile.StoredFileTitle,
                FolderName = folder.Name
            };

            _context.FileAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task RecordDeletionAsync(StoredFile storedFile, string userId)
        {
            var folder = await _context.Folder.FindAsync(storedFile.FolderId);
            if (folder == null)
            {
                throw new ArgumentException("Invalid folder ID.");
            }

            var audit = new FileAudit
            {
                FileId = storedFile.StoredFileId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ActionType = "Remoção",
                StoredFileTitle = storedFile.StoredFileTitle,
                FolderName = folder.Name
            };

            _context.FileAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task RecordEditAsync(StoredFile storedFile, string userId)
        {
            var folder = await _context.Folder.FindAsync(storedFile.FolderId);
            if (folder == null)
            {
                throw new ArgumentException("Invalid folder ID.");
            }

            var auditRecord = new FileAudit
            {
                FileId = storedFile.StoredFileId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ActionType = "Edição",
                StoredFileTitle = storedFile.StoredFileTitle,
                FolderName = folder.Name
            };

            _context.FileAudits.Add(auditRecord);
            await _context.SaveChangesAsync();
        }
    }
}
