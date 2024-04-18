using Microsoft.AspNetCore.Identity;
using PlataformaEDUGEP.Enums;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents a user of the application, extending the IdentityUser with custom properties specific to this platform.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Gets or sets the full name of the user, used for display purposes throughout the application.
        /// </summary>
        /// <value>
        /// The full name of the user as it should appear in user profiles and other user-facing interfaces.
        /// </value>
        [PersonalData]
        [Display(Name = "Nome de utilizador")]
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets a short description about the user which can be displayed in their profile.
        /// </summary>
        /// <value>
        /// A brief biography or description of the user, optionally provided by the user themselves.
        /// </value>
        [PersonalData]
        [Display(Name = "Acerca de mim")]
        public string? AboutMe { get; set; }

        /// <summary>
        /// Gets or sets the role of the user within the application, determining permissions and access.
        /// </summary>
        /// <value>
        /// The role assigned to the user which can be one of several predefined roles such as Student or Teacher.
        /// </value>
        public RoleType Role { get; set; }

        /// <summary>
        /// Gets or sets the collection of folders that the user has marked as liked, which can enhance personalized experiences.
        /// </summary>
        /// <value>
        /// A collection of FolderLike instances representing the folders that the user has expressed a preference for.
        /// </value>
        /// <remarks>
        /// This list can be used to customize user interfaces or recommend similar content, enhancing user engagement.
        /// </remarks>
        public virtual ICollection<FolderLike> LikedFolders { get; set; } = new List<FolderLike>();
    }
}
