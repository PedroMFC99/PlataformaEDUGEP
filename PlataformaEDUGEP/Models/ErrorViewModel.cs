namespace PlataformaEDUGEP.Models
{
    /// <summary>
    /// Represents the model for handling errors within the application.
    /// This model is typically used to pass error information to the view.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Gets or sets the unique request ID associated with the error, if any.
        /// </summary>
        /// <value>
        /// The request ID from the HTTP request that resulted in an error; null if the request did not generate an error.
        /// </value>
        public string? RequestId { get; set; }

        /// <summary>
        /// Determines whether the request ID should be shown in the error view.
        /// </summary>
        /// <value>
        /// True if the request ID is not null or empty, indicating that there is a specific request associated with the error; otherwise, false.
        /// </value>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
