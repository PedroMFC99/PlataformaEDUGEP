namespace PlataformaEDUGEP.Services
{
    /// <summary>
    /// Defines a service contract for recording audit trails for folder-related actions.
    /// </summary>
    public interface IFolderAuditService
    {
        /// <summary>
        /// Asynchronously logs an audit for a folder-related action.
        /// </summary>
        /// <param name="userId">The ID of the user who performed the action.</param>
        /// <param name="actionType">The type of action performed on the folder (e.g., 'Created', 'Deleted', 'Updated').</param>
        /// <param name="folderId">The ID of the folder on which the action was performed.</param>
        /// <param name="folderName">The name of the folder at the time of the action.</param>
        /// <returns>A task that represents the asynchronous operation of logging the folder audit.</returns>
        /// <remarks>
        /// This method is crucial for maintaining a reliable and secure log of all significant changes made to folders,
        /// allowing for enhanced security and administrative oversight.
        /// </remarks>
        Task LogAuditAsync(string userId, string actionType, int folderId, string folderName);
    }
}