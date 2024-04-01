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

        public DbSet<Tag> Tags { get; set; }

        public DbSet<Profile>? Profile { get; set; }

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

            // Configure the many-to-many relationship between Folder and Tag
            modelBuilder.Entity<Folder>()
                .HasMany(f => f.Tags)
                .WithMany(t => t.Folders)
                .UsingEntity(j => j.ToTable("FolderTags")); // This creates a join table named FolderTags
        }

    }
}