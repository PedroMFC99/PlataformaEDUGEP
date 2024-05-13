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
                .UseInMemoryDatabase(databaseName: dbName) // Make sure to use a unique name for each test run
                .Options;

            _context = new ApplicationDbContext(options);

            var folder = new Folder { FolderId = 1, Name = "Test Folder" };
            _context.Folder.Add(folder);
            _context.StoredFile.Add(new StoredFile
            {
                StoredFileId = 1,
                StoredFileName = "TestFile",
                StoredFileTitle = "Sample Title",
                UserId = "1",  // Assuming the 'UserId' is a string type as typically used with Identity
                FolderId = 1
            });
            _context.SaveChanges();


            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);

            _fileAuditServiceMock = new Mock<IFileAuditService>();

            var configurationMock = new Mock<IConfiguration>();
            var envMock = new Mock<IWebHostEnvironment>();

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
            // Ensure that the database is populated correctly.
            int expectedCount = _context.StoredFile.Count();
            Console.WriteLine($"Expected count directly from context: {expectedCount}");  // Output for debugging

            Assert.Equal(1, expectedCount);  // Verify that one file is expected from the setup.

            // Act
            var result = await _controller.Index();

            // Assert the type of result and the model it contains.
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StoredFile>>(viewResult.Model);
            Console.WriteLine($"Actual count from controller: {model.Count()}");  // Output for debugging

            // Check if the actual count matches the expected count.
            Assert.Equal(expectedCount, model.Count());
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
            // Arrange
            var fileName = "nonexistentfile.txt";
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(env => env.WebRootPath).Returns(Path.Combine("C:\\Temp\\WebRoot"));  // Ensure this is not null

            var controller = new StoredFilesController(_context, new Mock<IConfiguration>().Object, envMock.Object, _userManagerMock.Object, _fileAuditServiceMock.Object)
            {
                ControllerContext = _controller.ControllerContext
            };

            // Act
            var result = await controller.DownloadFile(fileName);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_FileExists_ReturnsFileResult()
        {
            // Arrange
            var fileName = "existingfile.txt";
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(env => env.WebRootPath).Returns(Path.Combine("C:\\Temp\\WebRoot"));

            var directoryPath = Path.Combine(envMock.Object.WebRootPath, "uploads");
            Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, fileName);

            try
            {
                // Create the file and immediately close it to free up the lock
                var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

                fileStream.Close();

                var controller = new StoredFilesController(_context, new Mock<IConfiguration>().Object, envMock.Object, _userManagerMock.Object, _fileAuditServiceMock.Object)
                {
                    ControllerContext = _controller.ControllerContext
                };

                // Act
                var result = await controller.DownloadFile(fileName);

                // Assert
                var fileResult = Assert.IsType<FileStreamResult>(result);
                Assert.Equal("text/plain", fileResult.ContentType); // Adjust the expected type
            }
            finally
            {
                const int maxAttemptCount = 3;
                for (int attempt = 0; attempt < maxAttemptCount; attempt++)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                        break; // If delete succeeded, exit the loop
                    }
                    catch (IOException)
                    {
                        if (attempt < maxAttemptCount - 1) // Wait only if another attempt is going to happen
                            Task.Delay(100).Wait(); // Wait before trying to delete the file again
                    }
                }
            }
        }

        [Fact]
        public async Task PreviewFile_ValidFile_ReturnsFileContentResult()
        {
            // Arrange
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(m => m.WebRootPath).Returns("C:\\Temp\\WebRoot");

            string fileName = "guid_testfile.txt"; // Ensure the file name has an underscore for parsing
            string filePath = Path.Combine(envMock.Object.WebRootPath, "uploads", fileName);
            string contentType = "text/plain"; // Assuming text/plain for .txt files

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));  // Ensure the directory exists
            await File.WriteAllTextAsync(filePath, "Sample content");

            var controller = new StoredFilesController(_context, new Mock<IConfiguration>().Object, envMock.Object, _userManagerMock.Object, _fileAuditServiceMock.Object)
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

            // Act
            var result = await controller.PreviewFile(fileName);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.Equal("Sample content", System.Text.Encoding.UTF8.GetString(fileResult.FileContents));

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task PreviewFile_FileDoesNotExist_ReturnsNotFoundResult()
        {
            // Arrange
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(m => m.WebRootPath).Returns("C:\\Temp\\WebRoot");

            var controller = new StoredFilesController(_context, new Mock<IConfiguration>().Object, envMock.Object, _userManagerMock.Object, _fileAuditServiceMock.Object)
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

            string fileName = "nonexistentfile.txt";

            // Act
            var result = await controller.PreviewFile(fileName);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_InvalidFolderId_ReturnsErrorView()
        {
            // Arrange
            var invalidFolderId = 999; // Assume 999 is an ID that does not exist in the database.

            // Act
            var result = _controller.Create(invalidFolderId);

            // Assert
            var viewResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Error404", viewResult.ActionName); // Verify it redirects to the Error404 action.
            Assert.Equal("Home", viewResult.ControllerName); // Verify it redirects to the Home controller.
        }

    }
}
