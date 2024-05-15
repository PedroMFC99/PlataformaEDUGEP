using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Services;
using PlataformaEDUGEP.AuxilliaryClasses;

namespace PlataformaEDUGEP.Tests
{
    public class FileAuditServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly ApplicationDbContext _context;
        private readonly FileAuditService _fileAuditService;

        public FileAuditServiceTests()
        {
            // Set up the in-memory database
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(_options);
            _fileAuditService = new FileAuditService(_context);
        }

        [Fact]
        public async Task RecordCreationAsync_AddsAuditCorrectly()
        {
            // Arrange
            var storedFile = new StoredFile
            {
                StoredFileId = 1,
                StoredFileTitle = "Test Document",
                FolderId = 1
            };
            _context.Folder.Add(new Folder { FolderId = 1, Name = "Test Folder" });
            _context.SaveChanges();

            // Act
            await _fileAuditService.RecordCreationAsync(storedFile, "user1");

            // Assert
            var audit = await _context.FileAudits.FirstOrDefaultAsync();
            Assert.NotNull(audit);
            Assert.Equal("Criação", audit.ActionType);
            Assert.Equal("Test Document", audit.StoredFileTitle);
            Assert.Equal("Test Folder", audit.FolderName);
        }

        [Fact]
        public async Task RecordDeletionAsync_AddsAuditCorrectly()
        {
            // Arrange
            var storedFile = new StoredFile
            {
                StoredFileId = 2,
                StoredFileTitle = "Delete Document",
                FolderId = 1
            };
            _context.Folder.Add(new Folder { FolderId = 1, Name = "Test Folder" });
            _context.SaveChanges();

            // Act
            await _fileAuditService.RecordDeletionAsync(storedFile, "user2");

            // Assert
            var audit = await _context.FileAudits.FirstOrDefaultAsync();
            Assert.NotNull(audit);
            Assert.Equal("Exclusão", audit.ActionType);
            Assert.Equal("Delete Document", audit.StoredFileTitle);
            Assert.Equal("Test Folder", audit.FolderName);
        }

        [Fact]
        public async Task RecordEditAsync_AddsAuditCorrectly()
        {
            // Arrange
            var storedFile = new StoredFile
            {
                StoredFileId = 3,
                StoredFileTitle = "Edit Document",
                FolderId = 1
            };
            _context.Folder.Add(new Folder { FolderId = 1, Name = "Test Folder" });
            _context.SaveChanges();

            // Act
            await _fileAuditService.RecordEditAsync(storedFile, "user3");

            // Assert
            var audit = await _context.FileAudits.FirstOrDefaultAsync();
            Assert.NotNull(audit);
            Assert.Equal("Edição", audit.ActionType);
            Assert.Equal("Edit Document", audit.StoredFileTitle);
            Assert.Equal("Test Folder", audit.FolderName);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
