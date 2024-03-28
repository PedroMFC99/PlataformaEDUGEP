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
using PlataformaEDUGEP.Services;

namespace PlataformaEDUGEP.Controllers
{
    public class FoldersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFolderAuditService _folderAuditService;

        public FoldersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFolderAuditService folderAuditService)
        {
            _context = context;
            _userManager = userManager;
            _folderAuditService = folderAuditService;
        }

        [Authorize]
        public async Task<IActionResult> Index(string searchString, string sortOrder, int[] selectedTagIds)
        {
            var userId = _userManager.GetUserId(User);

            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.CurrentFilter = searchString;
            ViewBag.SelectedTagIds = selectedTagIds; // Added for tag filtering

            // Ensure Tags SelectList is always passed to the view for the filter dropdown
            ViewBag.AllTags = new SelectList(await _context.Tags.ToListAsync(), "TagId", "Name");

            var folders = _context.Folder.Include(f => f.User).Include(f => f.Tags).AsQueryable();

            if (!User.IsInRole("Teacher") && !User.IsInRole("Admin"))
            {
                folders = folders.Where(f => !f.IsHidden);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                folders = folders.Where(s => s.Name.Contains(searchString));
            }

            // Adjusted Tag filtering logic for AND condition
            if (selectedTagIds != null && selectedTagIds.Length > 0)
            {
                folders = folders.Where(f => f.Tags.Count(t => selectedTagIds.Contains(t.TagId)) == selectedTagIds.Length);
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


        [HttpGet]
        public async Task<IActionResult> GetTags(string searchTerm)
        {
            var tagsQuery = _context.Tags.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                tagsQuery = tagsQuery.Where(t => t.Name.Contains(searchTerm));
            }

            var tags = await tagsQuery.Select(tag => new { id = tag.TagId, text = tag.Name }).ToListAsync();

            return Json(new { results = tags });
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
        [Authorize(Roles = "Teacher, Admin")]
        public IActionResult CreateModal()
        {
            ViewBag.TagItems = new SelectList(_context.Tags, "TagId", "Name");
            return PartialView("_CreatePartial", new Folder());
        }


        // POST: Folders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Folders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> Create([Bind("FolderId,Name,IsHidden")] Folder folder, [FromForm] int[] SelectedTagIds)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User); // Get the current user's ID
                folder.User = await _context.Users.FindAsync(userId); // Find the ApplicationUser by userId

                // Set CreationDate and ModificationDate to now
                folder.CreationDate = DateTime.Now;
                folder.ModificationDate = DateTime.Now;

                // Before adding the folder, associate it with selected tags
                if (SelectedTagIds != null && SelectedTagIds.Length > 0)
                {
                    // Retrieve the tags from the database based on the selected IDs
                    var tagsToAssociate = await _context.Tags
                        .Where(tag => SelectedTagIds.Contains(tag.TagId))
                        .ToListAsync();

                    // Initialize the Tags collection if null to avoid NullReferenceException
                    folder.Tags = new List<Tag>();

                    // Add each tag to the Folder's Tags collection
                    foreach (var tag in tagsToAssociate)
                    {
                        folder.Tags.Add(tag);
                    }
                }

                _context.Add(folder);
                await _context.SaveChangesAsync();

                // Now that the folder has been saved, it has an ID. We can log the action.
                await _folderAuditService.LogAuditAsync(userId, "Criação", folder.FolderId, folder.Name);

                return RedirectToAction(nameof(Index));
            }

            // If the model state is not valid, or if we need to return to the form for any reason,
            // ensure the Tags list is repopulated for the form to render correctly.
            ViewBag.TagItems = new SelectList(await _context.Tags.ToListAsync(), "TagId", "Name");

            return View(folder);
        }


        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Folder == null)
            {
                return NotFound();
            }

            var folder = await _context.Folder.Include(f => f.Tags).FirstOrDefaultAsync(m => m.FolderId == id);
            if (folder == null)
            {
                return NotFound();
            }
            ViewBag.TagItems = new SelectList(_context.Tags, "TagId", "Name"); // All tags for Select2
            ViewBag.SelectedTags = folder.Tags.Select(t => t.TagId).ToList(); // IDs of selected tags

            // Check if the request is for a partial view to be loaded in a modal
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", folder);
            }

            return View(folder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("FolderId,Name,IsHidden")] Folder folder, [FromForm] int[] SelectedTagIds)
        {
            if (id != folder.FolderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var folderToUpdate = await _context.Folder.Include(f => f.Tags).FirstOrDefaultAsync(f => f.FolderId == id);

                    if (folderToUpdate == null)
                    {
                        return NotFound();
                    }

                    folderToUpdate.Name = folder.Name;
                    folderToUpdate.IsHidden = folder.IsHidden;
                    // Update the ModificationDate to now
                    folderToUpdate.ModificationDate = DateTime.Now;

                    // Update tags
                    folderToUpdate.Tags.Clear();
                    var selectedTags = _context.Tags.Where(t => SelectedTagIds.Contains(t.TagId)).ToList();
                    foreach (var tag in selectedTags)
                    {
                        folderToUpdate.Tags.Add(tag);
                    }

                    await _context.SaveChangesAsync();

                    // Log the edit action after successfully saving changes
                    await _folderAuditService.LogAuditAsync(_userManager.GetUserId(User), "Edição", folder.FolderId, folder.Name);
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
        [Authorize(Roles = "Teacher, Admin")]
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
        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var folder = await _context.Folder.FindAsync(id);
            if (folder == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            // Log the delete action before actually removing the folder
            await _folderAuditService.LogAuditAsync(userId, "Exclusão", folder.FolderId, folder.Name);

            _context.Folder.Remove(folder);
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
            var userIsAdmin = User.IsInRole("Admin");

            var userLikes = _context.FolderLikes
                                    .Where(fl => fl.UserId == userId)
                                    .Include(fl => fl.Folder)
                                    .ThenInclude(folder => folder.User)
                                    .Select(fl => fl.Folder)
                                    .AsQueryable(); // Ensure this query remains IQueryable for further filtering

            // Filter out hidden folders for non-teachers
            if (!userIsTeacher && !userIsAdmin)
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditLog(string searchUser = "", string searchAction = "", string searchFolderName = "", string sortOrder = "")
        {
            ViewData["CurrentFilterUser"] = searchUser;
            ViewData["CurrentFilterAction"] = searchAction;
            ViewData["CurrentFilterFolderName"] = searchFolderName;
            ViewData["CurrentSort"] = sortOrder;

            var auditsQuery = from audit in _context.FolderAudits
                              join user in _context.Users on audit.UserId equals user.Id
                              select new FolderAuditViewModel
                              {
                                  FolderAuditId = audit.FolderAuditId,
                                  UserName = user.FullName, // Using the FullName
                                  ActionType = audit.ActionType,
                                  ActionTimestamp = audit.ActionTimestamp,
                                  FolderId = audit.FolderId,
                                  FolderName = audit.FolderName
                              };

            if (!String.IsNullOrEmpty(searchUser))
            {
                auditsQuery = auditsQuery.Where(a => a.UserName.Contains(searchUser));
            }
            if (!String.IsNullOrEmpty(searchAction))
            {
                auditsQuery = auditsQuery.Where(a => a.ActionType.Contains(searchAction));
            }
            if (!String.IsNullOrEmpty(searchFolderName))
            {
                auditsQuery = auditsQuery.Where(a => a.FolderName.Contains(searchFolderName));
            }

            switch (sortOrder)
            {
                case "time_asc":
                    auditsQuery = auditsQuery.OrderBy(a => a.ActionTimestamp);
                    break;
                case "time_desc":
                    auditsQuery = auditsQuery.OrderByDescending(a => a.ActionTimestamp);
                    break;
                default:
                    auditsQuery = auditsQuery.OrderByDescending(a => a.ActionTimestamp); // Default sorting
                    break;
            }

            var audits = await auditsQuery.ToListAsync();

            // Check if the request is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Return the partial view for AJAX requests
                return PartialView("_AuditLogTablePartial", audits);
            }

            return View(audits);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Ensure only admins can perform this action
        public async Task<IActionResult> ClearAuditLog()
        {
            var allAudits = _context.FolderAudits.ToList();
            _context.FolderAudits.RemoveRange(allAudits);
            await _context.SaveChangesAsync();

            // You can return a simple JSON result indicating success or redirect as needed
            return Json(new { success = true, message = "Audit log cleared successfully." });
        }

    }
}
