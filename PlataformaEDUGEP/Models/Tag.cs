using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents a tag that can be assigned to folders to categorize or describe them.
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// Gets or sets the unique identifier for the tag.
        /// </summary>
        /// <value>
        /// The primary key of the tag in the database.
        /// </value>
        [Key]
        public int TagId { get; set; }

        /// <summary>
        /// Gets or sets the name of the tag.
        /// </summary>
        /// <value>
        /// The name of the tag as it will appear in the user interface and be used in searches.
        /// </value>
        [Required]
        [Display(Name = "Tag Name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the collection of folders associated with this tag.
        /// </summary>
        /// <value>
        /// A collection of folders that this tag is linked to, facilitating a many-to-many relationship.
        /// </value>
        /// <remarks>
        /// This property uses lazy loading to fetch the related folders only when necessary, helping improve performance.
        /// </remarks>
        public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    }
}
