﻿using Microsoft.AspNetCore.Authorization;
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
        private readonly IWebHostEnvironment _env;

        public FoldersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFolderAuditService folderAuditService, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _folderAuditService = folderAuditService;
            _env = env;
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


        public async Task<IActionResult> SearchFolders(string searchString)
        {
            var foldersQuery = _context.Folder.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                foldersQuery = foldersQuery.Where(f => f.Name.Contains(searchString));
            }

            var folders = await foldersQuery.Select(f => new { f.FolderId, f.Name }).ToListAsync();

            return Json(folders);
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
        public async Task<IActionResult> Details(int? id, string fileTitle, string addedBy, string sortOrder)
        {
            if (id == null)
            {
                return NotFound();
            }

            var folder = await _context.Folder
                .Include(f => f.User)
                .Include(f => f.StoredFiles).ThenInclude(sf => sf.User) // Include User for each StoredFile
                .Include(f => f.Tags) // Include the tags
                .FirstOrDefaultAsync(m => m.FolderId == id);

            if (folder == null)
            {
                return NotFound();
            }

            // Fetch all folders for the dropdown list in the edit modal
            var folders = await _context.Folder.Select(f => new { f.FolderId, f.Name }).ToListAsync();
            ViewBag.FoldersJson = Newtonsoft.Json.JsonConvert.SerializeObject(folders);

            // Apply filters based on search criteria, ensuring case-insensitive comparison
            if (!string.IsNullOrEmpty(fileTitle))
            {
                folder.StoredFiles = folder.StoredFiles
                    .Where(sf => sf.StoredFileTitle.ToLower().Contains(fileTitle.ToLower()))
                    .ToList();
            }
            if (!string.IsNullOrEmpty(addedBy))
            {
                folder.StoredFiles = folder.StoredFiles
                    .Where(sf => sf.User.FullName.ToLower().Contains(addedBy.ToLower()))
                    .ToList();
            }

            // Sorting logic
            switch (sortOrder)
            {
                case "Date":
                    folder.StoredFiles = folder.StoredFiles.OrderBy(sf => sf.UploadDate).ToList();
                    ViewBag.UploadDateSortIcon = "↑";
                    ViewBag.NextSortOrder = "date_desc";
                    break;
                case "date_desc":
                    folder.StoredFiles = folder.StoredFiles.OrderByDescending(sf => sf.UploadDate).ToList();
                    ViewBag.UploadDateSortIcon = "↓";
                    ViewBag.NextSortOrder = "Date";
                    break;
                default:
                    folder.StoredFiles = folder.StoredFiles.OrderByDescending(sf => sf.UploadDate).ToList(); // Default sorting
                    ViewBag.UploadDateSortIcon = "↓";
                    ViewBag.NextSortOrder = "Date";
                    break;
            }

            // Pass the current sorting and filter criteria back to the view via ViewBag
            ViewBag.CurrentSortOrder = sortOrder;
            ViewBag.CurrentFileTitle = fileTitle;
            ViewBag.CurrentAddedBy = addedBy;

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

            return PartialView("_EditPartial", folder);
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
        // POST: Folders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var folder = await _context.Folder.Include(f => f.StoredFiles).FirstOrDefaultAsync(m => m.FolderId == id);
            if (folder == null)
            {
                return NotFound();
            }

            // Delete the files associated with the folder from the filesystem
            foreach (var file in folder.StoredFiles)
            {
                var filePath = Path.Combine(_env.WebRootPath, "uploads", file.StoredFileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Optionally, log each file deletion
            }

            // Now delete the files from the database
            _context.StoredFile.RemoveRange(folder.StoredFiles);

            // Log the delete action before actually removing the folder
            var userId = _userManager.GetUserId(User);
            await _folderAuditService.LogAuditAsync(userId, "Exclusão", folder.FolderId, folder.Name);

            // Remove the folder record from the database
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
                        .Include(fl => fl.Folder) // Include the Folder to then include the Tags
                            .ThenInclude(folder => folder.Tags) // This is crucial
                        .Select(fl => fl.Folder)
                        .AsQueryable();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> DeleteFromFavorites(int id)
        {
            var folder = await _context.Folder.FindAsync(id);
            if (folder != null)
            {
                var userId = _userManager.GetUserId(User); // Assuming you're tracking which user is performing the action

                // Log the delete action before actually removing the folder
                await _folderAuditService.LogAuditAsync(userId, "Exclusão", folder.FolderId, folder.Name);

                _context.Folder.Remove(folder);
                await _context.SaveChangesAsync();

                // Additional logging after successful deletion could be placed here if needed
            }

            // Redirect back to the Favorites page
            return RedirectToAction("Favorites");
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> DeleteConfirmedFavorites(int id)
        {
            var folder = await _context.Folder.FindAsync(id);
            if (folder == null)
            {
                return NotFound();
            }

            _context.Folder.Remove(folder);
            await _context.SaveChangesAsync();

            // Optionally, log the deletion here if you have audit logging in place.

            // Redirect to the Favorites page instead of the Index page
            return RedirectToAction(nameof(Favorites));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditLog(string searchUser = "", string searchAction = "", string searchFolderName = "", string sortOrder = "", int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilterUser"] = searchUser;
            ViewData["CurrentFilterAction"] = searchAction;
            ViewData["CurrentFilterFolderName"] = searchFolderName;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["CurrentPage"] = pageNumber;
            ViewData["PageSize"] = pageSize;

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

            int totalRecords = await auditsQuery.CountAsync();
            var audits = await auditsQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            // Use ViewData or ViewBag to pass pagination data
            ViewData["TotalRecords"] = totalRecords;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // For AJAX requests, return the partial view
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
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
