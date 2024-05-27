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
using System.IO;
using System.Threading.Tasks;
using PlataformaEDUGEP.Utilities;

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
        private readonly IBlobStorageService _blobStorageService;
        private readonly string _uploadsFolderPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoredFilesController"/> class.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="env">Web hosting environment.</param>
        /// <param name="userManager">User manager for handling user-related operations.</param>
        /// <param name="fileAuditService">Service for logging file-related actions.</param>
        /// <param name="blobStorageService">Service for handling blob storage operations.</param>
        public StoredFilesController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, IFileAuditService fileAuditService, IBlobStorageService blobStorageService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _fileAuditService = fileAuditService ?? throw new ArgumentNullException(nameof(fileAuditService));
            _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
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
            if (!folderId.HasValue)
            {
                return RedirectToAction("Error404", "Home");
            }

            var folderExists = _context.Folder.Any(f => f.FolderId == folderId.Value);
            if (!folderExists)
            {
                return RedirectToAction("Error404", "Home");
            }

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

            using (var stream = fileData.OpenReadStream())
            {
                await _blobStorageService.UploadFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), uniqueFileName, stream);
            }

            var storedFile = new StoredFile
            {
                StoredFileName = uniqueFileName,
                StoredFileTitle = storedFileName,
                UploadDate = TimeZoneHelper.ConvertUtcToLondonTime(DateTime.UtcNow),
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

            var stream = await _blobStorageService.DownloadFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), fileName);

            if (stream == null)
            {
                return NotFound();
            }

            var contentType = GetContentType(fileName);
            HttpContext.Response.ContentType = contentType;

            var originalFileName = fileName.Substring(fileName.IndexOf('_') + 1);
            ContentDispositionHeaderValue contentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = originalFileName
            };
            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

            return File(stream, contentType, originalFileName);
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

            var stream = await _blobStorageService.DownloadFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), fileName);

            if (stream == null)
            {
                return NotFound();
            }

            var originalFileName = fileName.Substring(fileName.IndexOf('_') + 1);
            var contentType = GetContentType(fileName);
            var contentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = originalFileName
            }.ToString();

            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition;
            return File(stream, contentType);
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

            var folders = _context.Folder.ToList();
            ViewBag.Folders = new SelectList(folders, "FolderId", "Name");

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

            var allowedExtensions = _configuration.GetSection("AllowedFileUploads:Extensions").Get<Dictionary<string, long>>();

            try
            {
                if (newFileData != null)
                {
                    var fileExtension = Path.GetExtension(newFileData.FileName).ToLower();
                    if (!allowedExtensions.ContainsKey(fileExtension) || newFileData.Length > allowedExtensions[fileExtension])
                    {
                        ModelState.AddModelError("newFileData", "The file type is not allowed or exceeds the maximum allowed size.");
                        return View(storedFile);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + storedFileTitle + fileExtension;

                    await _blobStorageService.DeleteFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), storedFile.StoredFileName);

                    using (var stream = newFileData.OpenReadStream())
                    {
                        await _blobStorageService.UploadFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), uniqueFileName, stream);
                    }

                    storedFile.StoredFileName = uniqueFileName;
                }
                else
                {
                    var oldFileExtension = Path.GetExtension(storedFile.StoredFileName);
                    var newFileName = Guid.NewGuid().ToString() + "_" + storedFileTitle + oldFileExtension;

                    await _blobStorageService.CopyFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), storedFile.StoredFileName, newFileName);
                    await _blobStorageService.DeleteFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), storedFile.StoredFileName);

                    storedFile.StoredFileName = newFileName;
                }

                storedFile.StoredFileTitle = storedFileTitle;
                storedFile.LastEditorFullName = editorFullName;
                storedFile.UploadDate = TimeZoneHelper.ConvertUtcToLondonTime(DateTime.UtcNow);
                storedFile.FolderId = folderId;

                _context.Update(storedFile);
                await _context.SaveChangesAsync();

                await _fileAuditService.RecordEditAsync(storedFile, userId);

                return Json(new { success = true, message = "File updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating the file." });
            }
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

            await _blobStorageService.DeleteFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), storedFile.StoredFileName);

            await _fileAuditService.RecordDeletionAsync(storedFile, userId);

            _context.StoredFile.Remove(storedFile);
            await _context.SaveChangesAsync();

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

            await _blobStorageService.DeleteFileAsync(_configuration.GetValue<string>("FileStorage:BlobContainerName"), storedFile.StoredFileName);

            await _fileAuditService.RecordDeletionAsync(storedFile, userId);

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

            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewData["CurrentPage"] = page;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_FileAuditLogTablePartial", audits);
            }

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
                var allAudits = await _context.FileAudits.ToListAsync();
                _context.FileAudits.RemoveRange(allAudits);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "All records deleted successfully." });
            }
            catch (Exception ex)
            {
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