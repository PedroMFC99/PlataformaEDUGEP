using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using PlataformaEDUGEP.Controllers;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PlataformaEDUGEP.Tests
{
    public class TagsControllerTests : IAsyncLifetime
    {
        private readonly ApplicationDbContext _context;
        private readonly TagsController _controller;
        private readonly Mock<ICompositeViewEngine> _mockViewEngine;

        public TagsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Ensures each test uses a unique DB
                .Options;

            _context = new ApplicationDbContext(options);

            // Initialize the mock view engine
            _mockViewEngine = new Mock<ICompositeViewEngine>();
            _mockViewEngine
                .Setup(engine => engine.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((ActionContext context, string viewName, bool isMainPage) =>
                {
                    if (viewName == "NonExistentView")
                    {
                        return ViewEngineResult.NotFound(viewName, new List<string> { "NonExistentView" });
                    }
                    return ViewEngineResult.Found(viewName, Mock.Of<IView>());
                });

            // Initialize the controller and set up a default context
            _controller = new TagsController(_context, _mockViewEngine.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        public async Task InitializeAsync()
        {
            await _context.Database.EnsureCreatedAsync();
            SeedDatabase();
        }

        public async Task DisposeAsync()
        {
            await _context.Database.EnsureDeletedAsync();
        }

        private void SeedDatabase()
        {
            _context.Tags.AddRange(
                new Tag { TagId = 1, Name = "Test1" },
                new Tag { TagId = 2, Name = "Test2" }
            );
            _context.SaveChanges();
        }

        private void SetAjaxRequestHeaders()
        {
            _controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        }

        [Fact]
        public async Task Index_WithNoSearchParameter_ReturnsAllTags()
        {
            // Act
            var result = await _controller.Index(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Tag>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Index_WithSearchParameter_ReturnsFilteredTags()
        {
            // Arrange
            var search = "Test1";

            // Act
            var result = await _controller.Index(search);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Tag>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Index_AjaxRequest_ReturnsPartialView()
        {
            // Arrange
            SetAjaxRequestHeaders();

            // Act
            var result = await _controller.Index("Test1");

            // Assert
            Assert.IsType<PartialViewResult>(result);
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

        [Fact]
        public void Create_Get_ViewDoesNotExist_ReturnsRedirectToError404()
        {
            // Arrange
            _mockViewEngine
                .Setup(engine => engine.FindView(It.IsAny<ActionContext>(), "Create", false))
                .Returns(ViewEngineResult.NotFound("Create", new List<string> { "Create" }));

            // Act
            var result = _controller.Create();

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Error404", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        [Fact]
        public async Task Edit_Post_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentTagId = 999;
            var tag = new Tag { TagId = nonExistentTagId, Name = "Updated Tag" };

            // Act
            var result = await _controller.Edit(nonExistentTagId, tag);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_TagNotFound_ReturnsNotFound()
        {
            // Arrange
            var nonExistentTagId = 999;

            // Act
            var result = await _controller.Delete(nonExistentTagId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
