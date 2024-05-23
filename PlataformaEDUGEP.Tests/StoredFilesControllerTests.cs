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
        private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
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
            _configurationMock.Setup(c => c.GetSection("FileStorage:BlobContainerName").Value).Returns("test-container");

            // Mock da seção AllowedFileUploads:Extensions manualmente
            var allowedFileUploadsSectionMock = new Mock<IConfigurationSection>();
            allowedFileUploadsSectionMock.Setup(a => a.Value)
                .Returns("{ \".pdf\": 10485760, \".png\": 5242880, \".jpg\": 5242880 }");

            var sectionMock = new Mock<IConfigurationSection>();
            sectionMock.Setup(x => x.GetChildren())
                .Returns(new List<IConfigurationSection> { allowedFileUploadsSectionMock.Object });

            _configurationMock.Setup(c => c.GetSection("AllowedFileUploads:Extensions")).Returns(sectionMock.Object);

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.ContentRootPath).Returns("C:\\Temp");

            _blobStorageServiceMock = new Mock<IBlobStorageService>();

            _uploadsFolderPath = Path.Combine("C:\\Temp", "uploads");

            Directory.CreateDirectory(_uploadsFolderPath);

            _controller = new StoredFilesController(_context, _configurationMock.Object, _envMock.Object, _userManagerMock.Object, _fileAuditServiceMock.Object, _blobStorageServiceMock.Object)
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
        public async Task Details_IdIsNull_ReturnsNotFoundResult()
        {
            var result = await _controller.Details(null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileNameIsNull_ReturnsNotFoundResult()
        {
            var result = await _controller.DownloadFile(null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileDoesNotExist_ReturnsNotFoundResult()
        {
            _blobStorageServiceMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((Stream)null);
            var result = await _controller.DownloadFile("nonexistentfile.txt");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileExists_ReturnsFileResult()
        {
            var fileName = "testfile.txt";
            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));

            _blobStorageServiceMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), fileName)).ReturnsAsync(fileStream);

            var result = await _controller.DownloadFile(fileName);

            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("text/plain", fileResult.ContentType); // Ajustado para "text/plain"
            Assert.Equal(fileName.Substring(fileName.IndexOf('_') + 1), fileResult.FileDownloadName);
        }

        [Fact]
        public async Task PreviewFile_ValidFile_ReturnsFileContentResult()
        {
            var fileName = "guid_testfile.txt";
            var fileContent = Encoding.UTF8.GetBytes("Sample content");
            var memoryStream = new MemoryStream(fileContent);

            _blobStorageServiceMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), fileName)).ReturnsAsync(memoryStream);

            var result = await _controller.PreviewFile(fileName);

            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("text/plain", fileResult.ContentType); // Ajustado para "text/plain"
        }

        [Fact]
        public async Task PreviewFile_FileDoesNotExist_ReturnsNotFoundResult()
        {
            var fileName = "nonexistentfile.txt";
            _blobStorageServiceMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), fileName)).ReturnsAsync((Stream)null);

            var result = await _controller.PreviewFile(fileName);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Create_InvalidFolderId_ReturnsErrorView()
        {
            var invalidFolderId = 999;
            var result = _controller.Create(invalidFolderId);
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Error404", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        public void Dispose()
        {
            if (Directory.Exists(_uploadsFolderPath))
            {
                Directory.Delete(_uploadsFolderPath, true);
            }
        }
    }
}
