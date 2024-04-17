using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    public class ProfilesControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProfilesController _controller;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public ProfilesControllerTests()
        {
            // Mocking UserManager with necessary setup for the constructor
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Setup DbContext with InMemoryDatabase
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Ensure each test method runs with its own database instance
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            SeedDatabase();

            // Initialize controller with mocked UserManager and actual DbContext
            _controller = new ProfilesController(_context, _userManagerMock.Object);
        }

        private void SeedDatabase()
        {
            _context.Users.AddRange(
                new ApplicationUser { Id = "a1", UserName = "user1", FullName = "User One" },
                new ApplicationUser { Id = "b2", UserName = "user2", FullName = "User Two" }
            );

            _context.SaveChanges();

            _context.Profile.AddRange(
                new Profile { Id = 1, User = _context.Users.FirstOrDefault(u => u.Id == "a1") },
                new Profile { Id = 2, User = _context.Users.FirstOrDefault(u => u.Id == "b2") }
            );

            _context.SaveChanges();
        }

        [Fact]
        public async Task Index_ReturnsAViewResult_WithAListOfProfiles()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Profile>>(viewResult.Model);
            Assert.Equal(2, model.Count()); // Verifying that exactly two profiles are returned
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
        public async Task Create_Post_ValidData_ReturnsRedirectToActionResult()
        {
            // Arrange
            var newUser = new ApplicationUser { Id = "c3", UserName = "user3", FullName = "User Three" };
            var newProfile = new Profile { Id = 3, User = newUser };

            // Act
            var result = await _controller.Create(newProfile);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task Edit_Post_ValidData_ReturnsRedirectToActionResult()
        {
            // Arrange
            var profileToUpdate = _context.Profile.Find(1);
            profileToUpdate.User.FullName = "Updated Name";

            // Act
            var result = await _controller.Edit(profileToUpdate.Id, profileToUpdate);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_ValidId_RemovesProfileAndRedirects()
        {
            // Arrange
            var profileIdToDelete = 1;

            // Act
            var result = await _controller.DeleteConfirmed(profileIdToDelete);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.False(_context.Profile.Any(p => p.Id == profileIdToDelete));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
