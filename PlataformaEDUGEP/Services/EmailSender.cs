using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace WebPWrecover.Services
{
    /// <summary>
    /// Service for sending emails through SendGrid.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailSender"/> class.
        /// </summary>
        /// <param name="optionsAccessor">The configuration options for SendGrid.</param>
        /// <param name="logger">The logger used to log information about email operations.</param>
        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor,
                           ILogger<EmailSender> logger)
        {
            Options = optionsAccessor.Value;
            _logger = logger;
        }

        /// <summary>
        /// The SendGrid API key retrieved from application settings, used for sending emails.
        /// </summary>
        public AuthMessageSenderOptions Options { get; } // Set via Secret Manager.

        /// <summary>
        /// Asynchronously sends an email using the specified SendGrid API key.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="message">The message body of the email, both in plain text and HTML format.</param>
        /// <returns>A task that represents the asynchronous operation, containing information about the email operation success.</returns>
        /// <exception cref="Exception">Thrown when the SendGrid API key is not set.</exception>
        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(Options.SendGridKey))
            {
                throw new Exception("Null SendGridKey");
            }
            await Execute(Options.SendGridKey, subject, message, toEmail);
        }

        /// <summary>
        /// Executes the sending of an email through SendGrid's API.
        /// </summary>
        /// <param name="apiKey">The SendGrid API key.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="message">The message content of the email.</param>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <returns>A task that represents the asynchronous operation, with a log of the outcome.</returns>
        private async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("plataformagestaoedugepnoreply@gmail.com", "Plataforma Gestão EDUGEP NO-REPLY"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking to enhance privacy and avoid altering email content appearance.
            msg.SetClickTracking(false, false);

            var response = await client.SendEmailAsync(msg);
            _logger.LogInformation(response.IsSuccessStatusCode
                                   ? $"Email to {toEmail} queued successfully!"
                                   : $"Failure Email to {toEmail}");
        }
    }
}
