using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Data
{
    /// <summary>
    /// Represents the Entity Framework database context for the application, integrating Identity Framework for user management.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApplicationDbContext"/> with the specified options.
        /// </summary>
        /// <param name="options">The options to be used by a DbContext.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the database set for folders.
        /// </summary>
        public virtual DbSet<Folder> Folder { get; set; }

        /// <summary>
        /// Gets or sets the database set for stored files.
        /// </summary>
        public virtual DbSet<StoredFile> StoredFile { get; set; }

        /// <summary>
        /// Gets or sets the database set for folder likes.
        /// </summary>
        public DbSet<FolderLike> FolderLikes { get; set; }

        /// <summary>
        /// Gets or sets the database set for folder audits.
        /// </summary>
        public virtual DbSet<FolderAudit> FolderAudits { get; set; }

        /// <summary>
        /// Gets or sets the database set for file audits.
        /// </summary>
        public virtual DbSet<FileAudit> FileAudits { get; set; }

        /// <summary>
        /// Gets or sets the database set for tags.
        /// </summary>
        public virtual DbSet<Tag> Tags { get; set; }

        /// <summary>
        /// Gets or sets the database set for profiles.
        /// </summary>
        public virtual DbSet<Profile> Profile { get; set; }

        /// <summary>
        /// Configures the relationships between the database tables and initializes any additional configuration.
        /// </summary>
        /// <param name="modelBuilder">Defines the model for the context being created.</param>
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