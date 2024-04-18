namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// A view model representing an audit record for file actions within the application.
    /// This model is intended to provide a user-friendly view of the audit data, suitable for display in user interfaces.
    /// </summary>
    public class FileAuditViewModel
    {
        /// <summary>
        /// Gets or sets the identifier of the audit record.
        /// </summary>
        /// <value>
        /// The unique identifier for the audit record.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was recorded.
        /// </summary>
        /// <value>
        /// The exact date and time when the action was logged.
        /// </value>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who performed the action.
        /// </summary>
        /// <value>
        /// The user's name as it should appear in audit logs and reports.
        /// </value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the type of action that was taken on the file.
        /// </summary>
        /// <value>
        /// A string description of the action, such as "Created", "Updated", or "Deleted".
        /// </value>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the title of the file at the time of the audit.
        /// </summary>
        /// <value>
        /// The title of the file as it was stored in the audit log, which may differ from the current title if changes have occurred since then.
        /// </value>
        public string StoredFileTitle { get; set; }

        /// <summary>
        /// Gets or sets the name of the folder in which the audited file was located.
        /// </summary>
        /// <value>
        /// The folder name providing contextual information about the file's location at the time of the action.
        /// </value>
        public string FolderName { get; set; }
    }
}
