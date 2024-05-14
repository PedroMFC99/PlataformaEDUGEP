using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using PlataformaEDUGEP.Controllers;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Services;
using Xunit;

namespace PlataformaEDUGEP.Tests
{
    public class StoredFilesControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly StoredFilesController _controller;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IFileAuditService> _fileAuditServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly string _uploadsFolderPath;

        public StoredFilesControllerTests()
        {
            var dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var folder = new Folder { FolderId = 1, Name = "Test Folder" };
            _context.Folder.Add(folder);
            _context.StoredFile.Add(new StoredFile
            {
                StoredFileId = 1,
                StoredFileName = "TestFile",
                StoredFileTitle = "Sample Title",
                UserId = "1",
                FolderId = 1
            });
            _context.SaveChanges();

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);

            _fileAuditServiceMock = new Mock<IFileAuditService>();

            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c.GetSection("FileStorage:UploadsFolderPath").Value).Returns("uploads");

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.ContentRootPath).Returns("C:\\Temp");

            _uploadsFolderPath = Path.Combine("C:\\Temp", "uploads");

            // Ensure the uploads folder exists
            Directory.CreateDirectory(_uploadsFolderPath);

            _controller = new StoredFilesController(_context, _configurationMock.Object, _envMock.Object, _userManagerMock.Object, _fileAuditServiceMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "1"),
                            new Claim(ClaimTypes.Name, "testuser@example.com")
                        }, "mock"))
                    }
                }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithAllStoredFiles()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StoredFile>>(viewResult.Model);
            Assert.Single(model); // Check if there is exactly one stored file
        }

        [Fact]
        public async Task Details_IdIsNull_ReturnsNotFoundResult()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileNameIsNull_ReturnsNotFoundResult()
        {
            // Act
            var result = await _controller.DownloadFile(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileDoesNotExist_ReturnsNotFoundResult()
        {
            // Act
            var result = await _controller.DownloadFile("nonexistentfile.txt");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileExists_ReturnsFileResult()
        {
            // Arrange
            var fileName = "testfile.txt";
            var fullPath = Path.Combine(_uploadsFolderPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Ensure the file is created and immediately closed
            await File.WriteAllTextAsync(fullPath, "Hello World");

            // Act
            var result = await _controller.DownloadFile(fileName);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("text/plain", fileResult.ContentType);
            Assert.Equal(fileName, fileResult.FileDownloadName);

            // Cleanup
            fileResult.FileStream.Dispose();
            try
            {
                File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error cleaning up the file: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task PreviewFile_ValidFile_ReturnsFileContentResult()
        {
            // Arrange
            var fileName = "guid_testfile.txt"; // Ensure the file name has an underscore for parsing

            var uploadsPath = Path.Combine("C:\\Temp", "uploads");
            var fullPath = Path.Combine(uploadsPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Ensure the file is created and immediately closed
            await File.WriteAllTextAsync(fullPath, "Sample content");

            // Act
            var result = await _controller.PreviewFile(fileName);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/plain", fileResult.ContentType); // Adjust to "text/plain"
            Assert.Equal("Sample content", Encoding.UTF8.GetString(fileResult.FileContents));

            // Cleanup
            File.Delete(fullPath);
        }

        [Fact]
        public async Task PreviewFile_FileDoesNotExist_ReturnsNotFoundResult()
        {
            // Arrange
            var fileName = "nonexistentfile.txt";

            // Act
            var result = await _controller.PreviewFile(fileName);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Create_InvalidFolderId_ReturnsErrorView()
        {
            // Arrange
            var invalidFolderId = 999;

            // Act
            var result = _controller.Create(invalidFolderId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Error404", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        public void Dispose()
        {
            // Clean up the uploads folder after tests
            if (Directory.Exists(_uploadsFolderPath))
            {
                Directory.Delete(_uploadsFolderPath, true);
            }
        }
    }
}
