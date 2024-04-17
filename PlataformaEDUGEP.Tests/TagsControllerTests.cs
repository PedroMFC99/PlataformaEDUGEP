using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PlataformaEDUGEP.Controllers;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PlataformaEDUGEP.Tests
{
    public class TagsControllerTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private TagsController _controller;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())  // Ensures each test uses a unique DB
                .Options;

            _context = new ApplicationDbContext(options);
            await _context.Database.EnsureCreatedAsync();
            SeedDatabase();

            _controller = new TagsController(_context);
        }

        public async Task DisposeAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            if (!_context.Tags.Any())
            {
                _context.Tags.AddRange(
                    new Tag { TagId = 1, Name = "Test1" },
                    new Tag { TagId = 2, Name = "Test2" }
                );
                _context.SaveChanges();
            }
        }

        [Fact]
        public async Task Index_ReturnsAViewResult_WithAListOfTags()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Tag>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Details_ReturnsViewResult_WithATag()
        {
            // Arrange
            var tagId = 1;

            // Act
            var result = await _controller.Details(tagId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Tag>(viewResult.Model);
            Assert.Equal(tagId, model.TagId);
        }

        // Add more tests as necessary
        [Fact]
        public async Task Create_Post_ValidData_ReturnsRedirectToIndex()
        {
            // Arrange
            var newTag = new Tag { Name = "New Tag" };

            // Act
            var result = await _controller.Create(newTag);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.True(_context.Tags.Any(t => t.Name == "New Tag"));
        }

        [Fact]
        public async Task Edit_Post_ValidData_ReturnsRedirectToIndex()
        {
            // Arrange
            var existingTag = new Tag { TagId = 1, Name = "Updated Tag" };

            // Act
            var result = await _controller.Edit(existingTag.TagId, existingTag);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Updated Tag", _context.Tags.Find(1).Name);
        }

        [Fact]
        public async Task DeleteConfirmed_ValidId_RemovesTagAndRedirects()
        {
            // Arrange
            var tagIdToDelete = 1;

            // Act
            var result = await _controller.DeleteConfirmed(tagIdToDelete);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.False(_context.Tags.Any(t => t.TagId == tagIdToDelete));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }



    }
}
