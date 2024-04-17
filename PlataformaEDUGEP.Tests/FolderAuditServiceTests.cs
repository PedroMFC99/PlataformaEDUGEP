using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Services;
using Xunit;

namespace PlataformaEDUGEP.Tests
{
    public class FolderAuditServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FolderAuditService _folderAuditService;

        public FolderAuditServiceTests()
        {
            // Configure the in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())  // Ensure a fresh database
                .Options;

            _context = new ApplicationDbContext(options);
            _folderAuditService = new FolderAuditService(_context);
        }

        [Fact]
        public async Task LogAuditAsync_LogsCorrectInformation()
        {
            // Arrange
            string userId = "test-user";
            string actionType = "Create";
            int folderId = 1;
            string folderName = "Test Folder";

            // Act
            await _folderAuditService.LogAuditAsync(userId, actionType, folderId, folderName);

            // Assert
            var audit = await _context.FolderAudits.FirstOrDefaultAsync();
            Assert.NotNull(audit);
            Assert.Equal(userId, audit.UserId);
            Assert.Equal(actionType, audit.ActionType);
            Assert.Equal(folderId, audit.FolderId);
            Assert.Equal(folderName, audit.FolderName);
        }

        public void Dispose()
        {
            _context?.Dispose(); // Clean up the in-memory database
        }
    }
}
