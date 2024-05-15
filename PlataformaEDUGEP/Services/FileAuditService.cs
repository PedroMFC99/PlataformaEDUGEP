using PlataformaEDUGEP.AuxilliaryClasses;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Services
{
    /// <summary>
    /// Provides services for recording file-related actions (creation, deletion, edit) in the system's audit log.
    /// </summary>
    public class FileAuditService : IFileAuditService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileAuditService"/> class.
        /// </summary>
        /// <param name="context">The database context to be used for storing audit records.</param>
        public FileAuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Records the creation of a file in the audit log.
        /// </summary>
        /// <param name="storedFile">The file that was created.</param>
        /// <param name="userId">The ID of the user who created the file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RecordCreationAsync(StoredFile storedFile, string userId)
        {
            await RecordActionAsync(storedFile, userId, "Criação");
        }

        /// <summary>
        /// Records the deletion of a file in the audit log.
        /// </summary>
        /// <param name="storedFile">The file that was deleted.</param>
        /// <param name="userId">The ID of the user who deleted the file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RecordDeletionAsync(StoredFile storedFile, string userId)
        {
            await RecordActionAsync(storedFile, userId, "Exclusão");
        }

        /// <summary>
        /// Records the editing of a file in the audit log.
        /// </summary>
        /// <param name="storedFile">The file that was edited.</param>
        /// <param name="userId">The ID of the user who edited the file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RecordEditAsync(StoredFile storedFile, string userId)
        {
            await RecordActionAsync(storedFile, userId, "Edição");
        }

        /// <summary>
        /// Records an action taken on a file in the audit log.
        /// </summary>
        /// <param name="storedFile">The file involved in the action.</param>
        /// <param name="userId">The ID of the user who took the action.</param>
        /// <param name="actionType">The type of action taken.</param>
        /// <returns>A task that represents the asynchronous operation, adding the record to the database.</returns>
        /// <exception cref="ArgumentException">Thrown if the folder associated with the file cannot be found.</exception>
        private async Task RecordActionAsync(StoredFile storedFile, string userId, string actionType)
        {
            var folder = await _context.Folder.FindAsync(storedFile.FolderId);
            if (folder == null)
            {
                throw new ArgumentException("Invalid folder ID.");
            }

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