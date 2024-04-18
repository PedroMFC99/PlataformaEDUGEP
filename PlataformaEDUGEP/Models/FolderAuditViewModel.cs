namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// A view model representing a user-friendly version of a folder audit record for display in the application interface.
    /// This model is designed to simplify the presentation of folder audit details to users.
    /// </summary>
    public class FolderAuditViewModel
    {
        /// <summary>
        /// Gets or sets the identifier for the folder audit record.
        /// </summary>
        /// <value>
        /// The unique identifier for the folder audit entry.
        /// </value>
        public int FolderAuditId { get; set; }

        /// <summary>
        /// Gets or sets the full name of the user who performed the action.
        /// </summary>
        /// <value>
        /// The full name of the user, providing a more human-readable format than a user ID.
        /// </value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the type of action that was performed on the folder.
        /// </summary>
        /// <value>
        /// A string describing the action taken, such as "Created", "Updated", or "Deleted".
        /// </value>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was performed.
        /// </summary>
        /// <value>
        /// The exact date and time when the action was recorded, helping to trace the event chronologically.
        /// </value>
        public DateTime ActionTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the folder on which the action was taken.
        /// </summary>
        /// <value>
        /// The identifier of the affected folder, linking the audit entry to a specific folder.
        /// </value>
        public int FolderId { get; set; }

        /// <summary>
        /// Gets or sets the name of the folder at the time the action was taken.
        /// </summary>
        /// <value>
        /// The folder name, aiding in identifying the folder without needing additional data queries.
        /// </value>
        public string FolderName { get; set; }
    }
}
