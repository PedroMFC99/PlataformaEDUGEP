namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents a record of a user's "like" action on a specific folder within the application.
    /// This class facilitates tracking user interactions with folders, enhancing features such as the user list of favorite folders.
    /// </summary>
    public class FolderLike
    {
        /// <summary>
        /// Gets or sets the identifier of the user who liked the folder.
        /// </summary>
        /// <value>
        /// The unique identifier for the user, linking the like action to a specific user.
        /// </value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user who liked the folder.
        /// </summary>
        /// <value>
        /// The user object associated with this like action, providing direct access to user details.
        /// </value>
        public ApplicationUser User { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the folder that was liked.
        /// </summary>
        /// <value>
        /// The unique identifier for the folder, linking the like action to a specific folder.
        /// </value>
        public int FolderId { get; set; }

        /// <summary>
        /// Gets or sets the folder that was liked.
        /// </summary>
        /// <value>
        /// The folder object associated with this like action, providing direct access to folder details.
        /// </value>
        public Folder Folder { get; set; }
    }
}
