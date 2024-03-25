namespace PlataformaEDUGEP.Models
{
    public class FolderAuditViewModel
    {
        public int FolderAuditId { get; set; }
        public string UserName { get; set; } // User's full name
        public string ActionType { get; set; }
        public DateTime ActionTimestamp { get; set; }
        public int FolderId { get; set; }
        public string FolderName { get; set; }
    }

}
