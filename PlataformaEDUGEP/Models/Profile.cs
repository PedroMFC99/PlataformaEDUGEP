namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents a profile associated with a user in the application.
    /// This class may be used to extend user information and customization beyond the basic ApplicationUser properties.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Gets or sets the identifier for the Profile.
        /// </summary>
        /// <value>
        /// The unique identifier for the profile, typically used as a primary key in the database.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ApplicationUser associated with this profile.
        /// </summary>
        /// <value>
        /// The ApplicationUser object that this profile enhances or details further.
        /// </value>
        public ApplicationUser User { get; set; }
    }
}
