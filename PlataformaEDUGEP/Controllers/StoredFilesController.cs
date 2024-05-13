using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Services;

namespace PlataformaEDUGEP.Controllers
{
    /// <summary>
    /// Controller responsible for handling operations related to stored files.
    /// </summary>
    public class StoredFilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileAuditService _fileAuditService;
        private readonly string _uploadPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoredFilesController"/> class.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="env">Web hosting environment.</param>
        /// <param name="userManager">User manager for handling user-related operations.</param>
        /// <param name="fileAuditService">Service for logging file-related actions.</param>
        public StoredFilesController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, IFileAuditService fileAuditService)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
            _userManager = userManager;
            _fileAuditService = fileAuditService;

            try
            {
                var uploadsPathSetting = _configuration["FileStorage:UploadsFolderPath"];
                if (string.IsNullOrEmpty(uploadsPathSetting))
                {
                    throw new InvalidOperationException("Uploads folder path is not configured.");
                }
                _uploadPath = Path.Combine(_env.ContentRootPath, uploadsPathSetting);
                Directory.CreateDirectory(_uploadPath);  // Safe to call multiple times for the same path
            }
            catch (Exception ex)
            {
                // Log the exception or handle it according to your error handling policies
                Console.WriteLine($"Error initializing upload path: {ex.Message}");
            }
        }

        /// <summary>
        /// Displays the index page listing all stored files.
        /// </summary>
        /// <returns>The index view.</returns>
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.StoredFile.Include(s => s.Folder);
            return View(await applicationDbContext.ToListAsync());
        }

        /// <summary>
        /// Displays the details of a specific stored file.
        /// </summary>
        /// <param name="id">The ID of the stored file.</param>
        /// <returns>The details view for the stored file.</returns>
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.StoredFile == null)
            {
                return NotFound();
            }

            var storedFile = await _context.StoredFile
                .Include(s => s.Folder)
                .FirstOrDefaultAsync(m => m.StoredFileId == id);
            if (storedFile == null)
            {
                return NotFound();
            }

            return View(storedFile);
        }

         /// <summary>
        /// Displays the view to create a new stored file.
        /// </summary>
        /// <param name="folderId">The ID of the folder where the file will be stored.</param>
        /// <returns>The create view.</returns>
        [Authorize(Roles = "Admin, Teacher")]
        public IActionResult Create(int? folderId)
        {
            // Check if folderId is provided
            if (!folderId.HasValue)
            {
                // Redirect to Error404 page if no folderId is provided
                return RedirectToAction("Error404", "Home");
            }

            // Check if the folder exists
            var folderExists = _context.Folder.Any(f => f.FolderId == folderId.Value);
            if (!folderExists)
            {
                // Redirect to Error404 page if the folder does not exist
                return RedirectToAction("Error404", "Home");
            }

            // Proceed as normal if the folder exists
            var model = new StoredFile { FolderId = folderId.Value };
            return View(model);
        }

        /// <summary>
        /// Processes the request to create a new stored file.
        /// </summary>
        /// <param name="fileData">The file data uploaded by the user.</param>
        /// <param name="storedFileName">The name to store the file as.</param>
        /// <param name="folderId">The ID of the folder where the file will be stored.</param>
        /// <returns>A redirect to the created file's details page or returns to the create view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Create(IFormFile fileData, string storedFileName, int? folderId)
        {
            if (!folderId.HasValue || fileData == null || fileData.Length == 0)
            {
                ModelState.AddModelError("", "The FolderId and file data are required.");
                return View();
            }

            // Load allowed extensions and sizes from configuration
            var allowedExtensions = _configuration.GetSection("AllowedFileUploads:Extensions").Get<Dictionary<string, long>>();

            var fileExtension = Path.GetExtension(fileData.FileName).ToLower();
            if (!allowedExtensions.ContainsKey(fileExtension) || fileData.Length > allowedExtensions[fileExtension])
            {
                ModelState.AddModelError("fileData", "The file type is not allowed or exceeds the maximum allowed size.");
                return View();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Error", "Home");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + storedFileName + fileExtension;
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileData.CopyToAsync(stream);
            }

            var storedFile = new StoredFile
            {
                StoredFileName = uniqueFileName,
                StoredFileTitle = storedFileName,
                UploadDate = DateTime.Now,
                FolderId = folderId.Value,
                UserId = userId
            };

            _context.Add(storedFile);
            await _context.SaveChangesAsync();

            await _fileAuditService.RecordCreationAsync(storedFile, userId);

            return RedirectToAction("Details", "Folders", new { id = folderId });
        }

        /// <summary>
        /// Provides functionality to download a file stored in the system.
        /// </summary>
        /// <param name="fileName">The name of the file to download.</param>
        /// <returns>The file as a downloadable object.</returns>
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var path = Path.Combine(_uploadPath, fileName);

            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var contentType = GetContentType(path);
            HttpContext.Response.ContentType = contentType;

            // Extract the original file name for the Content-Disposition header
            var originalFileName = fileName.Substring(fileName.IndexOf('_') + 1);
            ContentDispositionHeaderValue contentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = originalFileName
            };
            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

            // Stream the file directly to the response
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return File(fileStream, contentType, originalFileName);
        }

        /// <summary>
        /// Retrieves the MIME content type of a file based on its file extension.
        /// </summary>
        /// <param name="path">The file path for which to retrieve the content type.</param>
        /// <returns>The MIME type as a string. Defaults to 'application/octet-stream' if the type cannot be determined.</returns>
        private string GetContentType(string path)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        /// <summary>
        /// Provides a file preview for files stored on the server.
        /// </summary>
        /// <param name="fileName">The name of the file to preview.</param>
        /// <returns>A file result that renders the file inline in the browser, if found; otherwise, a NotFound result.</returns>
        public async Task<IActionResult> PreviewFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var path = Path.Combine(_uploadPath, fileName);
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            // Check if the fileName contains '_'
            int underscoreIndex = fileName.IndexOf('_');
            if (underscoreIndex == -1 || underscoreIndex >= fileName.Length - 1)
            {
                // Handle the error or adjust the logic
                return NotFound(); // or another appropriate action
            }

            var originalFileName = fileName.Substring(underscoreIndex + 1);

            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            string contentType = GetContentType(path);
            var contentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = originalFileName
            }.ToString();

            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition;
            return File(memoryStream.ToArray(), contentType);
        }


        /// <summary>
        /// Displays the view for editing an existing stored file.
        /// </summary>
        /// <param name="id">The ID of the stored file to edit.</param>
        /// <returns>The edit view for the stored file.</returns>
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.StoredFile == null)
            {
                return NotFound();
            }

            var storedFile = await _context.StoredFile
                .Include(s => s.Folder)
                .FirstOrDefaultAsync(m => m.StoredFileId == id);
            if (storedFile == null)
            {
                return NotFound();
            }

            // Fetch and pass folder list data to the view
            var folders = _context.Folder.ToList();
            ViewBag.Folders = new SelectList(_uploadPath, "Name");

            return View(storedFile);
        }

        /// <summary>
        /// Processes the request to edit an existing stored file.
        /// </summary>
        /// <param name="id">The ID of the stored file to update.</param>
        /// <param name="newFileData">The new file data if uploaded by the user.</param>
        /// <param name="storedFileTitle">The new title for the stored file.</param>
        /// <param name="folderId">The ID of the folder to move the file to, if changed.</param>
        /// <returns>A redirect to the updated file's details page or returns to the edit view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Edit(int id, IFormFile? newFileData, string storedFileTitle, int folderId)
        {
            var storedFile = await _context.StoredFile.FindAsync(id);
            if (storedFile == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(storedFile);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Error", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            var editorFullName = user?.FullName;

            // Load allowed extensions and sizes from configuration
            var allowedExtensions = _configuration.GetSection("AllowedFileUploads:Extensions").Get<Dictionary<string, long>>();

            if (newFileData != null)
            {
                var fileExtension = Path.GetExtension(newFileData.FileName).ToLower();
                if (!allowedExtensions.ContainsKey(fileExtension) || newFileData.Length > allowedExtensions[fileExtension])
                {
                    ModelState.AddModelError("newFileData", "The file type is not allowed or exceeds the maximum allowed size.");
                    return View(storedFile);
                }

                // Construct the new file name using storedFileTitle and file extension
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + storedFileTitle + fileExtension;
                var newFilePath = Path.Combine(_uploadPath, uniqueFileName);

                // Delete the old file
                var oldFilePath = Path.Combine(_uploadPath, storedFile.StoredFileName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Save the new file
                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await newFileData.CopyToAsync(stream);
                }

                // Update the StoredFile entity with the new file name
                storedFile.StoredFileName = uniqueFileName;
            }
            else
            {
                // If no new file is uploaded but the title is changed, generate a new file name with the old extension
                var oldFileExtension = Path.GetExtension(storedFile.StoredFileName);
                var newFileName = Guid.NewGuid().ToString() + "_" + storedFileTitle + oldFileExtension;
                var newFilePath = Path.Combine(_uploadPath, newFileName);
                var oldFilePath = Path.Combine(_uploadPath, storedFile.StoredFileName);

                // Rename the old file (if it still exists)
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Move(oldFilePath, newFilePath);
                }

                // Update the file name in the database to reflect the new title
                storedFile.StoredFileName = newFileName;
            }

            // Update other properties
            storedFile.StoredFileTitle = storedFileTitle;
            storedFile.LastEditorFullName = editorFullName;
            storedFile.UploadDate = DateTime.Now;
            storedFile.FolderId = folderId;

            try
            {
                _context.Update(storedFile);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!StoredFileExists(storedFile.StoredFileId))
                {
                    return NotFound();
                }
                else
                {
                    return Json(new { success = false, message = $"An error occurred while updating the file: {ex.Message}" });
                }
            }

            await _fileAuditService.RecordEditAsync(storedFile, userId);

            return Json(new { success = true, message = "File updated successfully." });
        }

        /// <summary>
        /// Displays the view for deleting a stored file.
        /// </summary>
        /// <param name="id">The ID of the stored file to delete.</param>
        /// <returns>The delete confirmation view.</returns>
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.StoredFile == null)
            {
                return NotFound();
            }

            var storedFile = await _context.StoredFile
                .Include(s => s.Folder)
                .FirstOrDefaultAsync(m => m.StoredFileId == id);
            if (storedFile == null)
            {
                return NotFound();
            }

            return View(storedFile);
        }

        /// <summary>
        /// Processes the request to delete a stored file from the system.
        /// </summary>
        /// <param name="id">The ID of the stored file to delete.</param>
        /// <returns>A redirect to the index page of stored files.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var storedFile = await _context.StoredFile.Include(s => s.Folder).FirstOrDefaultAsync(m => m.StoredFileId == id);
            if (storedFile == null)
            {
                return Problem("Entity set 'ApplicationDbContext.StoredFile' is null.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Error", "Home");
            }

            // Build the path to the file on the server
            var filePath = Path.Combine(_uploadPath, storedFile.StoredFileName);

            // Check if the file exists and delete it
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Record the file deletion action
            await _fileAuditService.RecordDeletionAsync(storedFile, userId);

            // Continue with the existing code to remove the record from the database
            _context.StoredFile.Remove(storedFile);
            await _context.SaveChangesAsync();

            // Redirect to the Details page of the associated folder
            return RedirectToAction("Details", "Folders", new { id = storedFile.FolderId });
        }

        /// <summary>
        /// Handles the deletion of a file via an AJAX request.
        /// </summary>
        /// <param name="id">The ID of the file to delete.</param>
        /// <returns>A JSON response indicating success or failure of the deletion.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> AjaxDeleteFile(int id)
        {
            var storedFile = await _context.StoredFile.FindAsync(id);
            if (storedFile == null)
            {
                return Json(new { success = false, message = "File not found." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Error", "Home");
            }

            // Build the path to the file on the server
            var filePath = Path.Combine(_uploadPath, storedFile.StoredFileName);

            // Check if the file exists and delete it
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Record the file deletion action
            await _fileAuditService.RecordDeletionAsync(storedFile, userId);

            // Remove the record from the database
            _context.StoredFile.Remove(storedFile);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "File deleted successfully." });
        }

        /// <summary>
        /// Displays a log of all file audits, with options for filtering and sorting.
        /// </summary>
        /// <param name="searchUser">Filter audits by user name.</param>
        /// <param name="searchAction">Filter audits by action type.</param>
        /// <param name="searchFileTitle">Filter audits by file title.</param>
        /// <param name="searchFolderName">Filter audits by folder name.</param>
        /// <param name="sortOrder">Sort order for the audit log entries.</param>
        /// <param name="page">Current page for pagination.</param>
        /// <param name="pageSize">Number of items per page for pagination.</param>
        /// <returns>The view displaying the filtered and sorted audit logs.</returns>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FileAuditLog(string searchUser = "", string searchAction = "", string searchFileTitle = "", string searchFolderName = "", string sortOrder = "time_desc", int page = 1, int pageSize = 10)
        {
            ViewData["CurrentFilterUser"] = searchUser;
            ViewData["CurrentFilterAction"] = searchAction;
            ViewData["CurrentFilterFileTitle"] = searchFileTitle;
            ViewData["CurrentFilterFolderName"] = searchFolderName;
            ViewData["CurrentSort"] = sortOrder;

            var auditsQuery = from audit in _context.FileAudits
                              join user in _context.Users on audit.UserId equals user.Id
                              select new FileAuditViewModel
                              {
                                  Id = audit.Id,
                                  Timestamp = audit.Timestamp,
                                  UserName = user.FullName,
                                  ActionType = audit.ActionType,
                                  StoredFileTitle = audit.StoredFileTitle,
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
            if (!String.IsNullOrEmpty(searchFileTitle))
            {
                auditsQuery = auditsQuery.Where(a => a.StoredFileTitle.Contains(searchFileTitle));
            }
            if (!String.IsNullOrEmpty(searchFolderName))
            {
                auditsQuery = auditsQuery.Where(a => a.FolderName.Contains(searchFolderName));
            }

            // Apply sort order to query
            switch (sortOrder)
            {
                case "time_asc":
                    auditsQuery = auditsQuery.OrderBy(a => a.Timestamp);
                    break;
                case "time_desc":
                    auditsQuery = auditsQuery.OrderByDescending(a => a.Timestamp);
                    break;
            }

            // Pagination
            int totalRecords = await auditsQuery.CountAsync();
            var audits = await auditsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // ViewData for pagination
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewData["CurrentPage"] = page;

            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Return only the partial view for the file audit log table
                return PartialView("_FileAuditLogTablePartial", audits);
            }

            // For non-AJAX requests, return the full view
            return View(audits);
        }

        /// <summary>
        /// Deletes all file audits from the system.
        /// </summary>
        /// <returns>A JSON response indicating success or failure of the deletion.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAllFileAudits()
        {
            try
            {
                // Fetch all records
                var allAudits = await _context.FileAudits.ToListAsync();
                // Remove all fetched records
                _context.FileAudits.RemoveRange(allAudits);
                // Save changes to the database
                await _context.SaveChangesAsync();

                // Optionally return a success message or redirect
                return Json(new { success = true, message = "All records deleted successfully." });
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g., log them and return an error response
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        /// <summary>
        /// Checks if a specific file exists in the database.
        /// </summary>
        /// <param name="id">The ID of the file to check.</param>
        /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
        [Authorize]
        private bool StoredFileExists(int id)
        {
          return (_context.StoredFile?.Any(e => e.StoredFileId == id)).GetValueOrDefault();
        }
    }
}
