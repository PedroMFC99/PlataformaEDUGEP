using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Controllers
{
    public class StoredFilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public StoredFilesController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
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
        public async Task<IActionResult> Create(IFormFile fileData, int? folderId)
        {
            // Log or debug the incoming request
            Console.WriteLine($"Received file data: {fileData?.FileName}, Folder ID: {folderId}");

            // Create a new StoredFile instance to populate from form data
            var storedFile = new StoredFile();

            // Check if folderId is provided and valid
            if (!folderId.HasValue)
            {
                ModelState.AddModelError("FolderId", "The FolderId is required.");
                return View(storedFile);
            }

            // Check if a file is uploaded
            if (fileData == null || fileData.Length == 0)
            {
                ModelState.AddModelError("FileData", "The file is required.");
                return View(storedFile);
            }

            // Process the uploaded file if it's present
            var uploadsFolderPath = Path.Combine(_env.WebRootPath, "uploads");
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileData.FileName);
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileData.CopyToAsync(stream);
            }

            // Populate the StoredFile instance with the appropriate data
            storedFile.StoredFileName = uniqueFileName;
            storedFile.UploadDate = DateTime.Now;
            storedFile.FolderId = folderId.Value;

            // Add and save the StoredFile instance to the database
            _context.Add(storedFile);
            await _context.SaveChangesAsync();

            // Redirect to the index action after successfully saving
            return RedirectToAction(nameof(Index));
        }




        // GET: StoredFiles/Edit/5
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.StoredFile == null)
            {
                return NotFound();
            }

            var storedFile = await _context.StoredFile.FindAsync(id);
            if (storedFile == null)
            {
                return NotFound();
            }
            ViewData["FolderId"] = new SelectList(_context.Folder, "FolderId", "Name", storedFile.FolderId);
            return View(storedFile);
        }

        // POST: StoredFiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("StoredFileId,StoredFileName,UploadDate,FolderId")] StoredFile storedFile)
        {
            if (id != storedFile.StoredFileId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(storedFile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StoredFileExists(storedFile.StoredFileId))
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
            ViewData["FolderId"] = new SelectList(_context.Folder, "FolderId", "Name", storedFile.FolderId);
            return View(storedFile);
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
            if (_context.StoredFile == null)
            {
                return Problem("Entity set 'ApplicationDbContext.StoredFile'  is null.");
            }
            var storedFile = await _context.StoredFile.FindAsync(id);
            if (storedFile != null)
            {
                _context.StoredFile.Remove(storedFile);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        private bool StoredFileExists(int id)
        {
          return (_context.StoredFile?.Any(e => e.StoredFileId == id)).GetValueOrDefault();
        }
    }
}
