using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents a folder within the system, which can contain files and be liked or tagged by users.
    /// </summary>
    public class Folder
    {
        /// <summary>
        /// Gets or sets the primary key for the Folder.
        /// </summary>
        /// <value>
        /// The unique identifier for the folder.
        /// </value>
        [Key]
        public int FolderId { get; set; }

        /// <summary>
        /// Gets or sets the name of the folder.
        /// </summary>
        /// <value>
        /// The name of the folder as displayed to the user. Required field.
        /// </value>
        [Required(ErrorMessage = "Por favor, introduza um nome para a pasta.")]
        [Display(Name = "Nome da pasta")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the user who owns or created the folder.
        /// </summary>
        /// <value>
        /// The user associated with the folder.
        /// </value>
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Gets or sets the date and time the folder was created.
        /// </summary>
        /// <value>
        /// The creation date and time of the folder.
        /// </value>
        [Display(Name = "Data de criação")]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the date and time the folder was last modified.
        /// </summary>
        /// <value>
        /// The last modification date and time of the folder.
        /// </value>
        [Display(Name = "Data de modificação")]
        public DateTime ModificationDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the folder is hidden from students.
        /// </summary>
        /// <value>
        /// True if the folder is hidden; otherwise, false.
        /// </value>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets the collection of files stored within the folder.
        /// </summary>
        /// <value>
        /// The collection of files.
        /// </value>
        public virtual ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();

        /// <summary>
        /// Gets or sets the collection of likes associated with the folder.
        /// </summary>
        /// <value>
        /// The collection of likes from different users.
        /// </value>
        public virtual ICollection<FolderLike> Likes { get; set; } = new List<FolderLike>();

        /// <summary>
        /// Gets or sets the collection of tags associated with the folder.
        /// </summary>
        /// <value>
        /// The collection of tags that categorize or describe the folder.
        /// </value>
        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
