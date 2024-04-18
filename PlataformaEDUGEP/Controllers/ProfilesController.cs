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
        /// Displays the main view of profiles listing all available profiles.
        /// </summary>
        /// <returns>The index view of profiles.</returns>
        public async Task<IActionResult> Index()
        {
            return _context.Profile != null ?
                          View(await _context.Profile.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.Profile'  is null.");
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
        /// Displays a form to create a new profile.
        /// </summary>
        /// <returns>A view containing the form for creating a profile.</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the creation of a profile.
        /// </summary>
        /// <param name="profile">The profile data to be saved.</param>
        /// <returns>Redirects to the index view if successful, otherwise returns the current view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id")] Profile profile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(profile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        /// <summary>
        /// Displays a form to edit an existing profile.
        /// </summary>
        /// <param name="id">The unique identifier for the profile to be edited.</param>
        /// <returns>The edit view for the specified profile or Not Found if no such profile exists.</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Profile == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }
            return View(profile);
        }

        /// <summary>
        /// Processes the update of a profile.
        /// </summary>
        /// <param name="id">The ID of the profile to update.</param>
        /// <param name="profile">The updated profile data.</param>
        /// <returns>Redirects to the index view if successful, otherwise returns the edit view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id")] Profile profile)
        {
            if (id != profile.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProfileExists(profile.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        /// <summary>
        /// Displays a confirmation dialog for deleting a profile.
        /// </summary>
        /// <param name="id">The unique identifier of the profile to delete.</param>
        /// <returns>The delete view for the specified profile or Not Found if no such profile exists.</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Profile == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        /// <summary>
        /// Processes the deletion of a profile after confirmation.
        /// </summary>
        /// <param name="id">The ID of the profile to delete.</param>
        /// <returns>Redirects to the index view if successful.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Profile == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Profile' is null.");
            }

            var profile = await _context.Profile.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }

            _context.Profile.Remove(profile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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