using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents a file stored in the system, detailing its metadata and associations.
    /// </summary>
    public class StoredFile
    {
        /// <summary>
        /// Gets or sets the identifier for the stored file.
        /// </summary>
        /// <value>
        /// The unique identifier for the file.
        /// </value>
        [Key]
        public int StoredFileId { get; set; }

        /// <summary>
        /// Gets or sets the name of the file as stored in the file system.
        /// </summary>
        /// <value>
        /// The name of the file including extension.
        /// </value>
        [Required]
        [Display(Name = "Nome do ficheiro")]
        public string StoredFileName { get; set; }

        /// <summary>
        /// Gets or sets the title of the file for display purposes.
        /// </summary>
        /// <value>
        /// A user-friendly title of the file.
        /// </value>
        [Required]
        [Display(Name = "Título do ficheiro")]
        public string StoredFileTitle { get; set; }

        /// <summary>
        /// Gets or sets the date and time the file was uploaded.
        /// </summary>
        /// <value>
        /// The upload date and time of the file.
        /// </value>
        [Display(Name = "Data de upload")]
        public DateTime UploadDate { get; set; }

        /// <summary>
        /// Gets or sets the form file data, not mapped to the database, used for file upload operations.
        /// </summary>
        /// <value>
        /// The file data received from a form upload, not stored in the database.
        /// </value>
        [NotMapped]
        public IFormFile FileData { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the folder containing this file.
        /// </summary>
        /// <value>
        /// The foreign key linking to the folder containing the file.
        /// </value>
        [ForeignKey("Folder")]
        public int FolderId { get; set; }

        /// <summary>
        /// Gets or sets the folder that contains this file.
        /// </summary>
        /// <value>
        /// The folder object containing this file, allowing navigation back to the folder details.
        /// </value>
        public virtual Folder Folder { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who uploaded the file.
        /// </summary>
        /// <value>
        /// The user ID of the user who uploaded the file.
        /// </value>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user who uploaded the file.
        /// </summary>
        /// <value>
        /// The user object associated with the uploader of the file.
        /// </value>
        public virtual ApplicationUser? User { get; set; }

        /// <summary>
        /// Gets or sets the full name of the last editor of the file.
        /// </summary>
        /// <value>
        /// The name of the last user to edit the file, enhancing traceability of modifications.
        /// </value>
        [Display(Name = "Última edição por")]
        public string? LastEditorFullName { get; set; }
    }
}
