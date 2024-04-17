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
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb") // Make sure to use a unique name for each test run
                .Options;

            _context = new ApplicationDbContext(options);

            // Populate the database with test data, ensuring all required fields are set
            _context.StoredFile.Add(new StoredFile
            {
                StoredFileId = 1,
                StoredFileName = "TestFile",
                StoredFileTitle = "Sample Title",
                UserId = "1"  // Assuming the 'UserId' is a string type as typically used with Identity
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
        public async Task Details_IdIsNull_ReturnsNotFoundResult()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
