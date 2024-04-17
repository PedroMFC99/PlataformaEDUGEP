using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using PlataformaEDUGEP.Controllers;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace PlataformaEDUGEP.Tests
{
    public class FoldersControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FoldersController _controller;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IFolderAuditService> _mockFolderAuditService;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;

        public FoldersControllerTests()
        {
            // Setup DbContext with In-Memory Database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase") // Ensure each test method has a unique name or use ClassInitialize to clear data
                .Options;

            _context = new ApplicationDbContext(options);
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);
            _mockFolderAuditService = new Mock<IFolderAuditService>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();

            _controller = new FoldersController(_context, _mockUserManager.Object, _mockFolderAuditService.Object, _mockWebHostEnvironment.Object);

            // Setup HttpContext to simulate User Identity
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task SearchFolders_ReturnsJson_WithFilteredData()
        {
            // Arrange
            _context.Folder.AddRange(
                new Folder { FolderId = 1, Name = "Test1" },
                new Folder { FolderId = 2, Name = "Sample" }
            );
            _context.SaveChanges();

            // Act
            var result = await _controller.SearchFolders("Test");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<dynamic>>(jsonResult.Value);
            Assert.Single(model); // Expecting one result that matches "Test"
        }

        [Fact]
        public async Task GetTags_ReturnsJson_WithFilteredData()
        {
            // Arrange
            _context.Tags.AddRange(
                new Tag { TagId = 1, Name = "Tag1" },
                new Tag { TagId = 2, Name = "Tag2" }
            );
            _context.SaveChanges();

            // Act
            var result = await _controller.GetTags("Tag1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);

            // Deserialize JSON to dynamic object
            var jsonContent = JsonConvert.SerializeObject(jsonResult.Value);
            dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonContent);

            // Asserting the properties of the JSON dynamically
            int count = data.results.Count;
            Assert.Equal(1, count); // Verify that there is exactly one result that matches "Tag1"
        }

    }
}
