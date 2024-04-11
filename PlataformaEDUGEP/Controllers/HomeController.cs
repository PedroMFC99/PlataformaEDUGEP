using Microsoft.AspNetCore.Mvc;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System.Diagnostics;

namespace PlataformaEDUGEP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var userCount = _context.Users.Count();
            var folderCount = _context.Folder.Count();
            var fileCount = _context.StoredFile.Count();

            // Assuming that you have DbSet<IdentityRole> Roles and DbSet<IdentityUserRole<string>> UserRoles in your DbContext
            var teacherRole = _context.Roles.FirstOrDefault(r => r.Name == "Teacher");
            var studentRole = _context.Roles.FirstOrDefault(r => r.Name == "Student");

            // Get the count of users in the 'Teacher' role
            var teacherCount = teacherRole != null
                ? _context.UserRoles.Count(ur => ur.RoleId == teacherRole.Id)
                : 0;

            // Get the count of users in the 'Student' role
            var studentCount = studentRole != null
                ? _context.UserRoles.Count(ur => ur.RoleId == studentRole.Id)
                : 0;

            ViewBag.UserCount = userCount;
            ViewBag.FolderCount = folderCount;
            ViewBag.FileCount = fileCount;
            ViewBag.TeacherCount = teacherCount;
            ViewBag.StudentCount = studentCount;

            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Error404()
        {
            return View();
        }
    }
}