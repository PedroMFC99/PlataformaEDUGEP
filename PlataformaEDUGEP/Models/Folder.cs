using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaEDUGEP.Models
{
    public class Folder
    {
        [Key]
        public int FolderId { get; set; }

        [Required(ErrorMessage = "Por favor, introduza um nome para a pasta.")]
        [Display(Name = "Nome da pasta")]
        public string? Name { get; set; }
        public ApplicationUser? User { get; set; }

        [Display(Name = "Data de criação")]
        public DateTime CreationDate { get; set; }

        [Display(Name = "Data de modificação")]
        public DateTime ModificationDate { get; set; }

        public bool IsHidden { get; set; }

        public virtual ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();

        public virtual ICollection<FolderLike> Likes { get; set; } = new List<FolderLike>();
    }
}