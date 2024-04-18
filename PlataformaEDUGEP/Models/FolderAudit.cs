namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents an audit record for folder-related actions performed within the system.
    /// This class is used to log and monitor operations such as creation, modification, or deletion of folders.
    /// </summary>
    public class FolderAudit
    {
        /// <summary>
        /// Gets or sets the identifier for the folder audit record.
        /// </summary>
        /// <value>
        /// The unique identifier for the folder audit entry.
        /// </value>
        public int FolderAuditId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who performed the action.
        /// </summary>
        /// <value>
        /// The user's identifier from the Identity system.
        /// </value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of action that was performed on the folder.
        /// </summary>
        /// <value>
        /// A string describing the action, such as "Created", "Updated", or "Deleted".
        /// Consider using an enum for more structured data if applicable.
        /// </value>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was performed.
        /// </summary>
        /// <value>
        /// The exact date and time when the action was logged.
        /// </value>
        public DateTime ActionTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the folder on which the action was taken.
        /// </summary>
        /// <value>
        /// The identifier of the affected folder.
        /// </value>
        public int FolderId { get; set; }

        /// <summary>
        /// Gets or sets the name of the folder at the time the action was taken.
        /// </summary>
        /// <value>
        /// The folder name, providing a quick reference to identify the folder without needing to fetch additional data.
        /// </value>
        public string FolderName { get; set; }

        // Include any other details you want to track about the folder action here.
        // For example, you might want to log changes to permissions or add notes about why the action was taken.
    }
}
