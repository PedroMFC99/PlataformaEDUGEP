using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PlataformaEDUGEP.Controllers;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Data;
using Microsoft.AspNetCore.Identity;
using PlataformaEDUGEP.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;

namespace PlataformaEDUGEP.Tests
{
    public class StoredFilesControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly StoredFilesController _controller;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IFileAuditService> _fileAuditServiceMock;

        public StoredFilesControllerTests()
        {
            var dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            _context = new ApplicationDbContext(options);

            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c["FileStorage:UploadsFolderPath"]).Returns("uploads");

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(env => env.ContentRootPath).Returns(Path.GetTempPath());

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);

            _fileAuditServiceMock = new Mock<IFileAuditService>();

            _controller = new StoredFilesController(_context, configurationMock.Object, envMock.Object, _userManagerMock.Object, _fileAuditServiceMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
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
            int expectedCount = _context.StoredFile.Count();
            var result = await _controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StoredFile>>(viewResult.Model);
            Assert.Equal(expectedCount, model.Count());
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
            var fileName = "nonexistentfile.txt";
            var result = await _controller.DownloadFile(fileName);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileExists_ReturnsFileResult()
        {
            var fileName = "existingfile.txt";
            var filePath = Path.Combine(Path.GetTempPath(), "uploads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, "Sample content");

            FileStreamResult fileResult = null;
            try
            {
                // Act
                var result = await _controller.DownloadFile(fileName);
                fileResult = Assert.IsType<FileStreamResult>(result);

                // Assert
                Assert.Equal("text/plain", fileResult.ContentType);
            }
            finally
            {
                int attempts = 0;
                while (attempts < 3)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        await Task.Delay(100);
                        attempts++;
                    }
                }
            }
        }


        [Fact]
        public async Task PreviewFile_ValidFile_ReturnsFileContentResult()
        {
            var fileName = "guid_testfile.txt";
            var filePath = Path.Combine(Path.GetTempPath(), "uploads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, "Sample content");

            var result = await _controller.PreviewFile(fileName);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/plain", fileResult.ContentType);
            Assert.Equal("Sample content", System.Text.Encoding.UTF8.GetString(fileResult.FileContents));

            File.Delete(filePath);
        }

        [Fact]
        public async Task PreviewFile_FileDoesNotExist_ReturnsNotFoundResult()
        {
            var fileName = "nonexistentfile.txt";
            var result = await _controller.PreviewFile(fileName);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_InvalidFolderId_ReturnsErrorView()
        {
            var invalidFolderId = 999;
            var result = _controller.Create(invalidFolderId);
            var viewResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Error404", viewResult.ActionName);
            Assert.Equal("Home", viewResult.ControllerName);
        }

    }
}
