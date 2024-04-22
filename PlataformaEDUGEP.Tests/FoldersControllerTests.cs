using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            var dbName = $"TestDatabase_{Guid.NewGuid()}"; // Unique database name for each instance
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
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

        public class ResultData
        {
            public List<TagResult> Results { get; set; }
        }

        public class TagResult
        {
            public int id { get; set; }
            public string text { get; set; }
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

        [Fact]
        public async Task CreateModal_ReturnsPartialView_WithCorrectData()
        {
            // Arrange
            _context.Tags.AddRange(
                new Tag { TagId = 1, Name = "Tag1" },
                new Tag { TagId = 2, Name = "Tag2" }
            );
            _context.SaveChanges();

            // Act
            var result = _controller.CreateModal();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_CreatePartial", partialViewResult.ViewName); // Verify that the correct view is returned

            var tagItems = Assert.IsType<SelectList>(partialViewResult.ViewData["TagItems"]);
            Assert.Equal(2, tagItems.Count()); // Verify that the correct number of tags is passed to the view

            // Optionally, check that the tags are correctly passed to the view
            var tagList = tagItems.Select(t => t.Text).ToList();
            Assert.Contains("Tag1", tagList);
            Assert.Contains("Tag2", tagList);
        }

        [Fact]
        public async Task DeleteConfirmed_FolderExists_DeletesFolderAndRedirects()
        {
            // Arrange
            var folderId = 1;
            var userId = "user1"; // Example user ID, must correspond to an actual or mocked user
            var testFolder = new Folder
            {
                FolderId = folderId,
                Name = "Test Folder",
                StoredFiles = new List<StoredFile>
        {
            new StoredFile
            {
                StoredFileId = 1,
                StoredFileName = "file1.txt",
                StoredFileTitle = "Test File", // Required property
                UserId = userId // Required property, this should be a valid or mocked user ID
            }
        }
            };

            _context.Folder.Add(testFolder);
            _context.SaveChanges();

            // Mock the environment setup if your method interacts with the file system
            _mockWebHostEnvironment.Setup(env => env.WebRootPath).Returns("C:\\fakepath");

            // Act
            var result = await _controller.DeleteConfirmed(folderId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName); // Redirects to Index after deletion
            Assert.False(_context.Folder.Any(f => f.FolderId == folderId)); // Folder should be deleted
            Assert.False(_context.StoredFile.Any(sf => sf.FolderId == folderId)); // Files should be deleted
        }

        [Fact]
        public async Task ToggleLike_TogglesLikeStatusAndRedirects()
        {
            // Arrange
            int folderId = 1;
            string userId = "user1";  // Ensure this user ID corresponds to an actual or mocked user.

            // Set up user with required properties
            var user = new ApplicationUser { Id = userId, UserName = "testUser", FullName = "Test User" }; // Include FullName

            // Ensure UserManager returns the correct user ID
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            // Add user to the context
            _context.Users.Add(user);
            _context.Folder.Add(new Folder { FolderId = folderId, Name = "Test Folder" }); // Ensure the folder exists.
            _context.SaveChanges();

            // Ensure no likes exist initially for clean state
            _context.FolderLikes.RemoveRange(_context.FolderLikes.Where(fl => fl.FolderId == folderId && fl.UserId == userId));
            _context.SaveChanges();

            // Act 1: Like the folder
            var resultLike = await _controller.ToggleLike(folderId);

            // Assert 1: Check if the like is added
            var redirectToActionResultLike = Assert.IsType<RedirectToActionResult>(resultLike);
            Assert.Equal("Index", redirectToActionResultLike.ActionName);  // Redirects to Index after liking
            Assert.True(_context.FolderLikes.Any(fl => fl.FolderId == folderId && fl.UserId == userId));

            // Act 2: Unlike the folder
            var resultUnlike = await _controller.ToggleLike(folderId);

            // Assert 2: Check if the like is removed
            var redirectToActionResultUnlike = Assert.IsType<RedirectToActionResult>(resultUnlike);
            Assert.Equal("Index", redirectToActionResultUnlike.ActionName);  // Redirects to Index after unliking
            Assert.False(_context.FolderLikes.Any(fl => fl.FolderId == folderId && fl.UserId == userId));
        }

        [Fact]
        public async Task GetTags_ReturnsCorrectTags_WhenCalledWithSearchTerm()
        {
            // Arrange
            _context.Tags.AddRange(
                new Tag { TagId = 1, Name = "Education" },
                new Tag { TagId = 2, Name = "EdTech" },
                new Tag { TagId = 3, Name = "Learning" }
            );
            _context.SaveChanges();

            var searchTerm = "Ed";

            // Act
            var result = await _controller.GetTags(searchTerm);
            var jsonResult = Assert.IsType<JsonResult>(result);
            var jsonContent = JsonConvert.SerializeObject(jsonResult.Value);
            var responseData = JsonConvert.DeserializeObject<ResultData>(jsonContent);

            // Assert
            Assert.NotNull(responseData);
            Assert.Equal(2, responseData.Results.Count); // Expecting two tags that match "Ed"
            Assert.Contains(responseData.Results, t => t.text == "Education");
            Assert.Contains(responseData.Results, t => t.text == "EdTech");
            Assert.DoesNotContain(responseData.Results, t => t.text == "Learning");
        }

        [Fact]
        public async Task Favorites_ReturnsFilteredFavoriteFolders_WhenCalledWithSearchString()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", UserName = "testUser", FullName = "Test User" };
            _context.Users.Add(user);

            var otherUser = new ApplicationUser { Id = "user2", UserName = "otherUser", FullName = "Other User" };
            _context.Users.Add(otherUser);

            var folders = new List<Folder>
    {
        new Folder { FolderId = 1, Name = "Education Tools", User = user },
        new Folder { FolderId = 2, Name = "EdTech Innovations", User = user },
        new Folder { FolderId = 3, Name = "Learning Platforms", User = otherUser }
    };
            _context.Folder.AddRange(folders);
            _context.FolderLikes.AddRange(
                new FolderLike { FolderId = 1, UserId = "user1" },
                new FolderLike { FolderId = 2, UserId = "user1" }
            );
            _context.SaveChanges();

            // Setup HttpContext to simulate User Identity as "user1"
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

            var searchString = "EdTech"; // Search term to filter folders

            // Act
            var result = await _controller.Favorites(searchString);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<Folder>>(viewResult.Model);
            Assert.Single(model); // Expecting one result that matches "EdTech"
            Assert.Contains(model, f => f.Name == "EdTech Innovations");
        }


    }
}