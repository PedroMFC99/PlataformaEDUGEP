namespace WebPWrecover.Services
{
    /// <summary>
    /// Represents configuration settings for authentication message sending services.
    /// </summary>
    public class AuthMessageSenderOptions
    {
        /// <summary>
        /// Gets or sets the API key for SendGrid, used for sending emails.
        /// </summary>
        /// <value>
        /// The SendGrid API key that authenticates requests to SendGrid services for sending emails.
        /// </value>
        public string? SendGridKey { get; set; }
    }
}