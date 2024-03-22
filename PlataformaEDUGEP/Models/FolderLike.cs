namespace PlataformaEDUGEP.Models
{
    public class FolderLike
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int FolderId { get; set; }
        public Folder Folder { get; set; }
    }

}
