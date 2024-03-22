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

        [Display(Name = "Data de upload")]
        public DateTime UploadDate { get; set; }

        [NotMapped] // This indicates EF Core to not map this property to the database.
        public IFormFile FileData { get; set; }

        [ForeignKey("Folder")]
        public int FolderId { get; set; }
        public Folder Folder { get; set; }
    }

}
