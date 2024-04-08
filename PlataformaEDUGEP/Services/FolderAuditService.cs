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
            TimeZoneInfo londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            // Convert from UTC to London time
            DateTime londonTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, londonTimeZone);

            var audit = new FolderAudit
            {
                UserId = userId,
                ActionType = actionType,
                ActionTimestamp = londonTime,
                FolderId = folderId,
                FolderName = folderName
            };

            _context.FolderAudits.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}
