using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System.Diagnostics;

namespace PlataformaEDUGEP.Controllers
{
    /// <summary>
    /// Manages user profiles within the application, providing methods for CRUD operations and details viewing.
    /// </summary>
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initializes a new instance of <see cref="ProfilesController"/> with necessary dependencies.
        /// </summary>
        /// <param name="context">The application's database context.</param>
        /// <param name="userManager">The user manager for handling users.</param>
        public ProfilesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Displays details for a specific profile.
        /// </summary>
        /// <param name="id">The unique identifier for the profile.</param>
        /// <returns>The details view for the specified profile or Not Found if the profile does not exist.</returns>
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleLabel = roles.Any(role => role == "Admin") ? "Administrador" :
                            roles.Any(role => role == "Teacher") ? "Professor" :
                            roles.Any(role => role == "Student") ? "Estudante" :
                            "Desconhecido";

            ViewBag.RoleLabel = roleLabel;
            ViewBag.IsStudent = roles.Contains("Student");

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool isAdminOrTeacher = userRoles.Contains("Teacher") || userRoles.Contains("Admin");

            var userFolders = await _context.Folder
                .Where(f => f.User.Id == id && (isAdminOrTeacher || !f.IsHidden))
                .ToListAsync();

            ViewBag.UserFolders = userFolders;

            return View(user);
        }

        /// <summary>
        /// Checks if a profile exists in the database.
        /// </summary>
        /// <param name="id">The ID of the profile to check.</param>
        /// <returns>True if the profile exists, false otherwise.</returns>
        private bool ProfileExists(int id)
        {
            return (_context.Profile?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}