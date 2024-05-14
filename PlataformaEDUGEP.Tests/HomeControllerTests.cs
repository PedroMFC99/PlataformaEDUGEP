using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Controllers;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace PlataformaEDUGEP.Tests
{
    public class HomeControllerTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Test_Database")
                .Options;

            var context = new ApplicationDbContext(options);

            // Seed the database with dummy data if needed
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new ApplicationUser { UserName = "user1", FullName = "User One" },
                    new ApplicationUser { UserName = "user2", FullName = "User Two" }
                );

                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public void Index_ReturnsViewWithCounts()
        {
            // Arrange
            var context = CreateContext();
            var controller = new HomeController(null, context);

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ViewData["UserCount"]);
            Assert.NotNull(result.ViewData["FolderCount"]);
            Assert.NotNull(result.ViewData["FileCount"]);
            Assert.NotNull(result.ViewData["TeacherCount"]);
            Assert.NotNull(result.ViewData["StudentCount"]);
        }

        [Fact]
        public void About_ReturnsView()
        {
            // Arrange
            var controller = new HomeController(null, null);

            // Act
            var result = controller.About() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Error404_ReturnsView()
        {
            // Arrange
            var controller = new HomeController(null, null);

            // Act
            var result = controller.Error404() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            // Arrange
            var context = CreateContext();
            var controller = new HomeController(null, context);

            // Mock the Activity.Current
            var activity = new Activity("TestActivity");
            activity.Start();
            Activity.Current = activity;

            // Act
            var result = controller.Error() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<ErrorViewModel>(result.Model);
            Assert.Equal(activity.Id, model.RequestId);
            Assert.False(string.IsNullOrEmpty(model.RequestId));

            // Cleanup
            activity.Stop();
            Activity.Current = null;
        }
    }
}
