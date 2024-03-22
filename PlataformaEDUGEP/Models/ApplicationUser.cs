using Microsoft.AspNetCore.Identity;
using PlataformaEDUGEP.Enums;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace PlataformaEDUGEP.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [Display(Name = "Nome de utilizador")]
        public string FullName { get; set; }

        [PersonalData]
        [Display(Name = "Acerca de mim")]
        public string? AboutMe { get; set; }

        public RoleType Role { get; set; }

        public virtual ICollection<FolderLike> LikedFolders { get; set; } = new List<FolderLike>();
    }

}
