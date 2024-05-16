using Microsoft.AspNetCore.Http;
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
using System.Security.Claims;
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
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
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
        public async Task Details_IdIsNull_ReturnsNotFoundResult()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_UserNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var nonExistentUserId = "non-existent";

            // Setup the mock to return null when the user is not found
            _userManagerMock.Setup(x => x.FindByIdAsync(nonExistentUserId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Details(nonExistentUserId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
