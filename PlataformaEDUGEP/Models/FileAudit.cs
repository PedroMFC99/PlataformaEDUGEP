namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents an audit record for actions taken on files within the application.
    /// This class is used to track and log actions such as creation, modification, or deletion of files.
    /// </summary>
    public class FileAudit
    {
        /// <summary>
        /// Gets or sets the identifier for the audit record.
        /// </summary>
        /// <value>
        /// The unique identifier for the audit entry.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was performed.
        /// </summary>
        /// <value>
        /// The date and time the action occurred.
        /// </value>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the user identifier of the user who performed the action.
        /// </summary>
        /// <value>
        /// The identifier of the user associated with the action.
        /// </value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of action that was performed on the file.
        /// </summary>
        /// <value>
        /// A string representing the type of action, such as "Created", "Updated", or "Deleted".
        /// </value>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the file identifier of the file on which the action was performed.
        /// </summary>
        /// <value>
        /// The identifier of the file affected by the action.
        /// </value>
        public int FileId { get; set; }

        /// <summary>
        /// Gets or sets the title of the file at the time the audit was recorded.
        /// </summary>
        /// <value>
        /// The title of the file, which may differ from the current title if it has been changed since the action.
        /// </value>
        public string StoredFileTitle { get; set; }

        /// <summary>
        /// Gets or sets the name of the folder in which the file was located when the action was taken.
        /// </summary>
        /// <value>
        /// The name of the folder containing the file, providing context for the file's location within the application.
        /// </value>
        public string FolderName { get; set; }
    }
}
