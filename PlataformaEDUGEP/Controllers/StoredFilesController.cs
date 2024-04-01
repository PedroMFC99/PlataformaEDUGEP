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

        public StoredFilesController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
            // Use folderId as needed to pre-select the folder in your view
            if (folderId.HasValue)
            {
                ViewData["FolderId"] = new SelectList(_context.Folder, "FolderId", "Name", folderId.Value);
            }
            else
            {
                ViewData["FolderId"] = new SelectList(_context.Folder, "FolderId", "Name");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Create([Bind("StoredFileName,FolderId")] StoredFile storedFile, IFormFile fileData)
        {
            if (fileData == null || fileData.Length == 0)
            {
                ModelState.AddModelError("FileData", "The file is required.");
            }

            if (ModelState.IsValid)
            {
                var uploadsFolderPath = _configuration.GetValue<string>("FileStorage:UploadsFolderPath");
                var fileName = Path.GetFileName(fileData.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), uploadsFolderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileData.CopyToAsync(stream);
                }

                storedFile.StoredFileName = fileName; // Consider saving filePath if you need to retrieve it later
                storedFile.UploadDate = DateTime.Now;

                _context.Add(storedFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["FolderId"] = new SelectList(_context.Folder, "FolderId", "Name", storedFile.FolderId);
            return View(storedFile);
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
