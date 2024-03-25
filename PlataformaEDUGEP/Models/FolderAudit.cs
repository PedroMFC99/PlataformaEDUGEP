namespace PlataformaEDUGEP.Models
{
    public class FolderAudit
    {   public int FolderAuditId { get; set; }
        public string UserId { get; set; }
        public string ActionType { get; set; } // Could be an enum
        public DateTime ActionTimestamp { get; set; }
        public int FolderId { get; set; }
        public string FolderName { get; set; } // Optional: track the name for easier identification
                                               // Other details you want to track...
    }
}
