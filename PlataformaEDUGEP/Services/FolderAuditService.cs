using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Services
{
    public class FolderAuditService : IFolderAuditService
    {
        private readonly ApplicationDbContext _context;

        public FolderAuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAuditAsync(string userId, string actionType, int folderId, string folderName)
        {
            var audit = new FolderAudit
            {
                UserId = userId,
                ActionType = actionType,
                ActionTimestamp = DateTime.UtcNow,
                FolderId = folderId,
                FolderName = folderName
            };

            _context.FolderAudits.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}
