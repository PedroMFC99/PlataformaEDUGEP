using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace PlataformaEDUGEP.Models
{
    public class Tag
    {
        [Key]
        public int TagId { get; set; }

        [Required]
        [Display(Name = "Tag Name")]
        public string Name { get; set; }

        // Navigation property for the many-to-many relationship with Folder
        public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    }
}
