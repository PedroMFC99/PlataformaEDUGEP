using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Controllers
{
    public class FoldersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FoldersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            var userId = _userManager.GetUserId(User);

            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.CurrentFilter = searchString;

            var folders = _context.Folder.Include(f => f.User).AsQueryable();

            if (!User.IsInRole("Teacher"))
            {
                folders = folders.Where(f => !f.IsHidden);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                folders = folders.Where(s => s.Name.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    folders = folders.OrderByDescending(s => s.Name);
                    break;
                case "Date":
                    folders = folders.OrderBy(s => s.CreationDate);
                    break;
                case "date_desc":
                    folders = folders.OrderByDescending(s => s.CreationDate);
                    break;
                default:
                    folders = folders.OrderBy(s => s.Name);
                    break;
            }

            var folderList = await folders.ToListAsync();

            if (!folderList.Any())
            {
                ViewBag.NoResultsFound = "Nenhuma pasta foi encontrada.";
            }

            var likedFolders = await _context.FolderLikes.Where(fl => fl.UserId == userId).Select(fl => fl.FolderId).ToListAsync();
            ViewBag.FolderLikeStatus = folderList.ToDictionary(folder => folder.FolderId, folder => likedFolders.Contains(folder.FolderId));

            return View(folderList);
        }






        // GET: Folders/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Folder == null)
            {
                return NotFound();
            }

            var folder = await _context.Folder
                .Include(f => f.StoredFiles) // This includes the files in the folder.
                .Include(f => f.User) // Make sure to include this line if it's not already there.
                .FirstOrDefaultAsync(m => m.FolderId == id);
            if (folder == null)
            {
                return NotFound();
            }

            return View(folder);
        }



        // GET: Folders/Create
        [Authorize(Roles = "Teacher")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Folders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Folders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create([Bind("FolderId,Name")] Folder folder)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User); // Get the current user's ID
                folder.User = await _context.Users.FindAsync(userId); // Find the ApplicationUser by userId

                // Set CreationDate and ModificationDate to now
                folder.CreationDate = DateTime.Now;
                folder.ModificationDate = DateTime.Now;

                _context.Add(folder);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(folder);
        }

        // GET: Folders/Edit/5
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Folder == null)
            {
                return NotFound();
            }

            var folder = await _context.Folder.FindAsync(id);
            if (folder == null)
            {
                return NotFound();
            }
            return View(folder);
        }

        // POST: Folders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("FolderId,Name,IsHidden")] Folder folder)
        {
            if (id != folder.FolderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingFolder = await _context.Folder.FirstOrDefaultAsync(f => f.FolderId == id);
                    if (existingFolder == null)
                    {
                        return NotFound();
                    }

                    existingFolder.Name = folder.Name;
                    existingFolder.IsHidden = folder.IsHidden; // Update IsHidden property
                    existingFolder.ModificationDate = DateTime.Now;

                    _context.Update(existingFolder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FolderExists(folder.FolderId))
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
            return View(folder);
        }



        // GET: Folders/Delete/5
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Folder == null)
            {
                return NotFound();
            }

            var folder = await _context.Folder
                .FirstOrDefaultAsync(m => m.FolderId == id);
            if (folder == null)
            {
                return NotFound();
            }

            return View(folder);
        }

        // POST: Folders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Folder == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Folder'  is null.");
            }
            var folder = await _context.Folder.FindAsync(id);
            if (folder != null)
            {
                _context.Folder.Remove(folder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [Authorize]
        private bool FolderExists(int id)
        {
            return (_context.Folder?.Any(e => e.FolderId == id)).GetValueOrDefault();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int folderId)
        {
            var userId = _userManager.GetUserId(User);
            var existingLike = await _context.FolderLikes
                .FirstOrDefaultAsync(fl => fl.FolderId == folderId && fl.UserId == userId);

            if (existingLike == null)
            {
                var newLike = new FolderLike { FolderId = folderId, UserId = userId };
                _context.FolderLikes.Add(newLike);
            }
            else
            {
                _context.FolderLikes.Remove(existingLike);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); // Adjust as needed
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleLikeFavorites(int folderId, string returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            var existingLike = await _context.FolderLikes
                .FirstOrDefaultAsync(fl => fl.FolderId == folderId && fl.UserId == userId);

            if (existingLike == null)
            {
                var newLike = new FolderLike { FolderId = folderId, UserId = userId };
                _context.FolderLikes.Add(newLike);
            }
            else
            {
                _context.FolderLikes.Remove(existingLike);
            }

            await _context.SaveChangesAsync();

            // Check if the returnUrl is not null and local to prevent redirect attacks
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Favorites"); // Redirect back to Favorites if no returnUrl is provided or if it's not a local URL
            }
        }


        [Authorize]
        public async Task<IActionResult> Favorites(string searchString)
        {
            var userId = _userManager.GetUserId(User); // Get current user ID
            var userIsTeacher = User.IsInRole("Teacher");

            var userLikes = _context.FolderLikes
                                    .Where(fl => fl.UserId == userId)
                                    .Include(fl => fl.Folder)
                                    .ThenInclude(folder => folder.User)
                                    .Select(fl => fl.Folder)
                                    .AsQueryable(); // Ensure this query remains IQueryable for further filtering

            // Filter out hidden folders for non-teachers
            if (!userIsTeacher)
            {
                userLikes = userLikes.Where(f => !f.IsHidden);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                userLikes = userLikes.Where(s => s.Name.Contains(searchString));
            }

            var folderList = await userLikes.ToListAsync();
            var folderLikeStatus = folderList.ToDictionary(folder => folder.FolderId, folder => true);

            ViewBag.FolderLikeStatus = folderLikeStatus;

            // Add a check here for no favorite folders
            if (!folderList.Any())
            {
                ViewBag.NoFavoritesMessage = "Não há nenhuma pasta nos seus favoritos.";
            }


            return View(folderList);
        }


        // Add a method to handle adding tags to folders
        //[HttpPost]
        //[Authorize(Roles = "Teacher")]
        //public async Task<IActionResult> AddTagToFolder(int folderId, string tagName)
        //{
        //    // Find or create the tag
        //    var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
        //    if (tag == null)
        //    {
        //        tag = new Tag { Name = tagName };
        //        _context.Tags.Add(tag);
        //        await _context.SaveChangesAsync(); // Save the tag to get an Id for it
        //    }

        //    // Add the relationship if it doesn't exist
        //    var folderTagExists = await _context.FolderTags
        //        .AnyAsync(ft => ft.FolderId == folderId && ft.TagId == tag.TagId);
        //    if (!folderTagExists)
        //    {
        //        var folderTag = new FolderTag { FolderId = folderId, TagId = tag.TagId };
        //        _context.FolderTags.Add(folderTag);
        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction(nameof(Details), new { id = folderId });
        //}

    }
}
