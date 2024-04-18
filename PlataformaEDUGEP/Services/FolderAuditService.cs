using PlataformaEDUGEP.AuxilliaryClasses;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Services
{
    /// <summary>
    /// Provides services for recording folder-related actions in the system's audit log.
    /// </summary>
    public class FolderAuditService : IFolderAuditService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderAuditService"/> class.
        /// </summary>
        /// <param name="context">The database context used for storing audit records.</param>
        public FolderAuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Asynchronously logs an audit record for a folder-related action.
        /// </summary>
        /// <param name="userId">The ID of the user who performed the action.</param>
        /// <param name="actionType">The type of action performed (e.g., 'Created', 'Deleted', 'Updated').</param>
        /// <param name="folderId">The ID of the folder on which the action was performed.</param>
        /// <param name="folderName">The name of the folder at the time of the action.</param>
        /// <returns>A task that represents the asynchronous operation of logging the audit.</returns>
        /// <remarks>
        /// This method handles the creation of an audit record, including the conversion of timestamps
        /// to London time, and persists this information to the database.
        /// </remarks>
        public async Task LogAuditAsync(string userId, string actionType, int folderId, string folderName)
        {
            // Use the helper class for time zone conversion
            DateTime londonTime = TimeZoneHelper.ConvertUtcToLondonTime(DateTime.UtcNow);

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