using PlataformaEDUGEP.Models;
using System.Threading.Tasks;

namespace PlataformaEDUGEP.Services
{
    /// <summary>
    /// Defines a service contract for recording audit trails for file-related actions.
    /// </summary>
    public interface IFileAuditService
    {
        /// <summary>
        /// Asynchronously records the creation of a file.
        /// </summary>
        /// <param name="storedFile">The file that was created.</param>
        /// <param name="userId">The ID of the user who created the file.</param>
        /// <returns>A task that represents the asynchronous operation of recording the file creation.</returns>
        Task RecordCreationAsync(StoredFile storedFile, string userId);

        /// <summary>
        /// Asynchronously records the deletion of a file.
        /// </summary>
        /// <param name="storedFile">The file that was deleted.</param>
        /// <param name="userId">The ID of the user who deleted the file.</param>
        /// <returns>A task that represents the asynchronous operation of recording the file deletion.</returns>
        Task RecordDeletionAsync(StoredFile storedFile, string userId);

        /// <summary>
        /// Asynchronously records the editing of a file.
        /// </summary>
        /// <param name="storedFile">The file that was edited.</param>
        /// <param name="userId">The ID of the user who edited the file.</param>
        /// <returns>A task that represents the asynchronous operation of recording the file edit.</returns>
        Task RecordEditAsync(StoredFile storedFile, string userId);
    }
}