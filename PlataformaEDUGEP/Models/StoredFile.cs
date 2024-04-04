using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace PlataformaEDUGEP.Models
{
    public class StoredFile
    {
        [Key]
        public int StoredFileId { get; set; }

        [Required]
        [Display(Name = "Nome do ficheiro")]
        public string StoredFileName { get; set; }

        [Required] // Add this if the title is required
        [Display(Name = "Título do ficheiro")]
        public string StoredFileTitle { get; set; }

        [Display(Name = "Data de upload")]
        public DateTime UploadDate { get; set; }

        [NotMapped] // This indicates EF Core to not map this property to the database.
        public IFormFile FileData { get; set; }

        [ForeignKey("Folder")]
        public int FolderId { get; set; }
        public virtual Folder Folder { get; set; }

        // Add user reference
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

}
