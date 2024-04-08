namespace PlataformaEDUGEP.Models
{
    public class FileAudit
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string ActionType { get; set; } // "Created", "Updated", "Deleted"
        public int FileId { get; set; }
        public string StoredFileTitle { get; set; }
        public string FolderName { get; set; }
    }
}
