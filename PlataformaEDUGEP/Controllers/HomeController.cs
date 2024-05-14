using Microsoft.AspNetCore.Mvc;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System.Diagnostics;

namespace PlataformaEDUGEP.Controllers
{
    /// <summary>
    /// Controller responsible for handling the home page and related actions.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">The logger for capturing logging information.</param>
        /// <param name="context">The database context used for data access operations.</param>
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Displays the home page.
        /// </summary>
        /// <returns>The index view populated with counts of users, folders, files, and role-specific user counts.</returns>
        public IActionResult Index()
        {
            // Get the total count of users
            var userCount = _context.Users.Count();

            // Identifying the Admin role
            var adminRole = _context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var adminCount = adminRole != null
                ? _context.UserRoles.Count(ur => ur.RoleId == adminRole.Id)
                : 0;

            // Subtracting the admin count from the total user count
            userCount -= adminCount;

            var folderCount = _context.Folder.Count();
            var fileCount = _context.StoredFile.Count();

            var teacherRole = _context.Roles.FirstOrDefault(r => r.Name == "Teacher");
            var studentRole = _context.Roles.FirstOrDefault(r => r.Name == "Student");

            var teacherCount = teacherRole != null
                ? _context.UserRoles.Count(ur => ur.RoleId == teacherRole.Id)
                : 0;

            var studentCount = studentRole != null
                ? _context.UserRoles.Count(ur => ur.RoleId == studentRole.Id)
                : 0;

            // Update the ViewBag to use the adjusted user count
            ViewBag.UserCount = userCount;
            ViewBag.FolderCount = folderCount;
            ViewBag.FileCount = fileCount;
            ViewBag.TeacherCount = teacherCount;
            ViewBag.StudentCount = studentCount;

            return View();
        }

        /// <summary>
        /// Displays the About page.
        /// </summary>
        /// <returns>The About view.</returns>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Handles HTTP errors by displaying a custom error view.
        /// </summary>
        /// <returns>An Error view displaying the error details.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Handles HTTP 404 errors by displaying a custom 404 error page.
        /// </summary>
        /// <returns>A custom 404 Error view.</returns>
        public IActionResult Error404()
        {
            return View();
        }
    }
}
