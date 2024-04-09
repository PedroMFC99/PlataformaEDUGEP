using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public class StoredFilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileAuditService _fileAuditService;

        public StoredFilesController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, IFileAuditService fileAuditService)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
            _userManager = userManager;
            _fileAuditService = fileAuditService;
        }

        // GET: StoredFiles
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.StoredFile.Include(s => s.Folder);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: StoredFiles/Details/5
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

        // GET: StoredFiles/Create
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Create(IFormFile fileData, string storedFileName, int? folderId)
        {
            if (!folderId.HasValue || fileData == null || fileData.Length == 0)
            {
                // Combine error messages for efficiency
                ModelState.AddModelError("", "The FolderId and file data are required.");
                return View();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Error", "Home");
            }

            // Generate the file path once and reuse
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileData.FileName);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", uniqueFileName);

            // Use using statement to ensure the stream is disposed of
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileData.CopyToAsync(stream);
            }

            // Initialize the model in one go
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

            // Record the file creation action
            await _fileAuditService.RecordCreationAsync(storedFile, userId);

            return RedirectToAction("Details", "Folders", new { id = folderId });
        }

        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var path = Path.Combine(_env.WebRootPath, "uploads", fileName);

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

        private string GetContentType(string path)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        public async Task<IActionResult> PreviewFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var path = Path.Combine(_env.WebRootPath, "uploads", fileName);
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            string contentType = GetContentType(path);
            // Explicitly set the Content-Disposition header to inline; filename="{originalFileName}"
            // Note: Ensure originalFileName does not expose the encrypted part if sensitive
            var originalFileName = Path.GetFileNameWithoutExtension(fileName).Substring(37); // Adjust as necessary based on your naming convention
            var contentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = originalFileName
            }.ToString();

            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition;
            return File(memoryStream.ToArray(), contentType);
        }


        // GET: StoredFiles/Details/5
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
            ViewBag.Folders = new SelectList(folders, "FolderId", "Name");

            return View(storedFile);
        }


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
                // If the model state is not valid, return to the view or handle the error as needed.
                return View(storedFile);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the user ID here
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Error", "Home");
            }
            // Assuming you have access to the UserManager and can get the current user's full name
            // This part assumes ApplicationUser has a FullName or similar property.
            // Adjust according to your user management logic.
            var user = await _userManager.GetUserAsync(User);
            var editorFullName = user?.FullName; // Use 'FullName' or equivalent property if available

            // Update the stored file properties
            storedFile.StoredFileTitle = storedFileTitle;
            storedFile.LastEditorFullName = editorFullName; // Update the LastEditorFullName with the current user's full name

            if (newFileData != null && newFileData.Length > 0)
            {
                // If there's a new file, process and update it as well

                // Delete or move the old file as needed
                var oldFilePath = Path.Combine(_env.WebRootPath, "uploads", storedFile.StoredFileName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Save the new file
                var originalFileName = Path.GetFileName(newFileData.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                var uploadsFolderPath = Path.Combine(_env.WebRootPath, "uploads");
                var newFilePath = Path.Combine(uploadsFolderPath, uniqueFileName);

                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await newFileData.CopyToAsync(stream);
                }

                storedFile.StoredFileName = uniqueFileName; // Update with the new file's name
                storedFile.UploadDate = DateTime.Now; // Optionally update the upload date
            }

            // Update the FolderId if it was changed.
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
                    // Log the error or handle it as needed
                    return Json(new { success = false, message = $"An error occurred while updating the file: {ex.Message}" });
                }
            }

            // Record the file update action
            await _fileAuditService.RecordEditAsync(storedFile, userId); // Assuming you have a method for editing

            // If the action method is called via AJAX, return a JSON response
            return Json(new { success = true, message = "File updated successfully." });
        }


        // GET: StoredFiles/Delete/5
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

        // POST: StoredFiles/Delete/5
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
            var filePath = Path.Combine(_env.WebRootPath, "uploads", storedFile.StoredFileName);

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
            var filePath = Path.Combine(_env.WebRootPath, "uploads", storedFile.StoredFileName);

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

        [HttpPost]
        [Authorize(Roles = "Admin")] // Ensure this action is secured and accessible only by authorized roles
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


        [Authorize]
        private bool StoredFileExists(int id)
        {
          return (_context.StoredFile?.Any(e => e.StoredFileId == id)).GetValueOrDefault();
        }
    }
}
