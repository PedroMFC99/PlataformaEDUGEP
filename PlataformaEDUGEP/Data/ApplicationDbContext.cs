using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Folder>? Folder { get; set; }
        public DbSet<StoredFile>? StoredFile { get; set; }
        public DbSet<FolderLike> FolderLikes { get; set; }
        public DbSet<FolderAudit> FolderAudits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many relationship
            modelBuilder.Entity<FolderLike>()
                .HasKey(fl => new { fl.UserId, fl.FolderId });

            modelBuilder.Entity<FolderLike>()
                .HasOne(fl => fl.User)
                .WithMany(u => u.LikedFolders)
                .HasForeignKey(fl => fl.UserId);

            modelBuilder.Entity<FolderLike>()
                .HasOne(fl => fl.Folder)
                .WithMany(f => f.Likes)
                .HasForeignKey(fl => fl.FolderId);
        }


    }
}