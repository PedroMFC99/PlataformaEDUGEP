namespace PlataformaEDUGEP.Models
{
    public class FileAuditViewModel
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; }
        public string ActionType { get; set; }
        public string StoredFileTitle { get; set; }
        public string FolderName { get; set; }
    }
}
