using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Services;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using PlataformaEDUGEP.Utilities;

namespace PlataformaEDUGEP.Controllers
{
    /// <summary>
    /// Handles all folder-related interactions within the platform.
    /// </summary>
    public class FoldersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFolderAuditService _folderAuditService;
        private readonly IWebHostEnvironment _env;
        private readonly BlobServiceClient _blobServiceClient;

        /// <summary>
        /// Constructor for the FoldersController.
        /// </summary>
        /// <param name="context">The application's database context.</param>
        /// <param name="userManager">Manager for handling user-related operations.</param>
        /// <param name="folderAuditService">Service for logging folder-related actions.</param>
        /// <param name="env">Provides information about the web hosting environment.</param>
        /// <param name="blobServiceClient">Client for accessing Azure Blob Storage.</param>
        public FoldersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFolderAuditService folderAuditService, IWebHostEnvironment env, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _userManager = userManager;
            _folderAuditService = folderAuditService;
            _env = env;
            _blobServiceClient = blobServiceClient;
        }

        /// <summary>
        /// Displays a list of all folders, potentially filtered and sorted.
        /// </summary>
        /// <param name="searchString">Search filter for folder names.</param>
        /// <param name="sortOrder">Sort order for the displayed list.</param>
        /// <param name="selectedTagIds">Filter for folders tagged with specific tags.</param>
        /// <returns>A view of the index page showing folders.</returns>
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
                    folders = folders.OrderByDescending(s => s.CreationDate);
                    break;
                case "date_desc":
                    folders = folders.OrderBy(s => s.CreationDate);
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

        /// <summary>
        /// Searches folders by name and returns a JSON list of folders that match the search term.
        /// </summary>
        /// <param name="searchString">The search string to filter folder names.</param>
        /// <returns>A JSON response containing folders that match the search criteria.</returns>
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

        /// <summary>
        /// Provides a JSON response containing tags that match the given search term.
        /// </summary>
        /// <param name="searchTerm">The search term to filter tags.</param>
        /// <returns>A JSON response with tags matching the search criteria.</returns>
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


        /// <summary>
        /// Displays details for a specific folder, including files and associated information.
        /// </summary>
        /// <param name="id">The folder ID for which details are displayed.</param>
        /// <param name="fileTitle">Filter for file titles within the folder.</param>
        /// <param name="addedBy">Filter for files added by specific users.</param>
        /// <param name="sortOrder">Sort order for files within the folder.</param>
        /// <returns>A view displaying folder details.</returns>
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

        /// <summary>
        /// Returns a modal view to create a new folder.
        /// </summary>
        /// <returns>A partial view representing the modal.</returns>
        [Authorize(Roles = "Teacher, Admin")]
        public IActionResult CreateModal()
        {
            ViewBag.TagItems = new SelectList(_context.Tags, "TagId", "Name");
            return PartialView("_CreatePartial", new Folder());
        }


        /// <summary>
        /// Handles the creation of a new folder.
        /// </summary>
        /// <param name="folder">The folder data bound from the form.</param>
        /// <param name="SelectedTagIds">Tags selected for the folder.</param>
        /// <returns>Redirects to the index view after creation or returns the view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher, Admin")]
        public async Task<IActionResult> Create([Bind("FolderId,Name,IsHidden")] Folder folder, [FromForm] int[] SelectedTagIds)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User); // Get the current user's ID
                folder.User = await _context.Users.FindAsync(userId); // Find the ApplicationUser by userId

                // Set CreationDate and ModificationDate to now, converted to London time
                folder.CreationDate = TimeZoneHelper.ConvertUtcToLondonTime(DateTime.UtcNow);
                folder.ModificationDate = TimeZoneHelper.ConvertUtcToLondonTime(DateTime.UtcNow);

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

        /// <summary>
        /// Returns a modal view for editing a folder.
        /// </summary>
        /// <param name="id">The ID of the folder to edit.</param>
        /// <returns>A partial view representing the edit modal if found, otherwise NotFound.</returns>
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

        /// <summary>
        /// Handles the submission of folder edits.
        /// </summary>
        /// <param name="id">The ID of the folder being edited.</param>
        /// <param name="folder">The folder data bound from the form.</param>
        /// <param name="SelectedTagIds">Tags selected for the folder.</param>
        /// <returns>Redirects to the index view if successful, otherwise returns the view with errors.</returns>
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
                    // Update the ModificationDate to now, converted to London time
                    folderToUpdate.ModificationDate = TimeZoneHelper.ConvertUtcToLondonTime(DateTime.UtcNow);

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

        // <summary>
        /// Displays a confirmation view for deleting a folder.
        /// </summary>
        /// <param name="id">The ID of the folder to delete.</param>
        /// <returns>The delete confirmation view.</returns>
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

        /// <summary>
        /// Handles the confirmed deletion of a folder, removing it and any associated files.
        /// </summary>
        /// <param name="id">The ID of the folder to delete.</param>
        /// <returns>Redirects to the index view after deletion.</returns>
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

            // Delete the files associated with the folder from the blob storage
            var containerClient = _blobServiceClient.GetBlobContainerClient("uploads");
            foreach (var file in folder.StoredFiles)
            {
                var blobClient = containerClient.GetBlobClient(file.StoredFileName);
                await blobClient.DeleteIfExistsAsync();

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

        /// <summary>
        /// Checks if a folder exists in the database.
        /// </summary>
        /// <param name="id">The ID of the folder to check.</param>
        /// <returns>True if the folder exists, false otherwise.</returns>
        [Authorize]
        private bool FolderExists(int id)
        {
            return (_context.Folder?.Any(e => e.FolderId == id)).GetValueOrDefault();
        }

        /// <summary>
        /// Toggles the like status of a folder for the current user.
        /// </summary>
        /// <param name="folderId">The ID of the folder to toggle like status.</param>
        /// <returns>Redirects back to the index view.</returns>
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

        /// <summary>
        /// Toggles the like status of a folder for the current user and redirects back to a specific returnUrl or defaults to the Favorites view.
        /// </summary>
        /// <param name="folderId">The ID of the folder to toggle like status.</param>
        /// <param name="returnUrl">The URL to redirect to after toggling the like status.</param>
        /// <returns>Redirects to the given returnUrl or to the Favorites view if no returnUrl is provided or if it's not valid.</returns>
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

        /// <summary>
        /// Provides an interface to manage favorite folders for a user.
        /// </summary>
        /// <param name="searchString">Optional filter for folder names.</param>
        /// <returns>A view displaying favorite folders.</returns>
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

        /// <summary>
        /// Deletes a folder from the user's favorites and redirects to the Favorites view.
        /// </summary>
        /// <param name="id">The ID of the folder to be removed from favorites.</param>
        /// <returns>Redirects to the Favorites view after the folder is removed.</returns>
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

        /// <summary>
        /// Confirms and processes the deletion of a folder from the Favorites list, redirecting to the Favorites view upon completion.
        /// </summary>
        /// <param name="id">The ID of the folder to be permanently deleted from favorites.</param>
        /// <returns>Redirects to the Favorites view after successful deletion.</returns>
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

        /// <summary>
        /// Manages the audit log for folder-related actions.
        /// </summary>
        /// <param name="searchUser">Filter for user names in logs.</param>
        /// <param name="searchAction">Filter for action types in logs.</param>
        /// <param name="searchFolderName">Filter for folder names in logs.</param>
        /// <param name="sortOrder">Sort order for the logs.</param>
        /// <param name="pageNumber">The current page number in pagination.</param>
        /// <param name="pageSize">The number of records per page in pagination.</param>
        /// <returns>A view displaying the audit log with optional filters and pagination.</returns>
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

        /// <summary>
        /// Clears the audit log for folders.
        /// </summary>
        /// <returns>A JSON result indicating the success or failure of the operation.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
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
