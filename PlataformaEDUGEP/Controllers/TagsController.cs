using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Controllers
{
    public class TagsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tags
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search)
        {
            var tags = _context.Tags.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                tags = tags.Where(t => EF.Functions.Like(t.Name, $"%{search}%"));  // Ensures case insensitivity depending on the database
            }

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_TagListPartial", await tags.ToListAsync());
            }

            return View(await tags.ToListAsync());
        }

        // GET: Tags/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            var tag = await GetTagById(id);
            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        // GET: Tags/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreateTagPartial"); // Return partial view for AJAX requests
            }
            return View();
        }

        // POST: Tags/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("TagId,Name")] Tag tag)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tags == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditTagPartial", tag); // Return partial view for AJAX requests
            }

            return View(tag);
        }


        // POST: Tags/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("TagId,Name")] Tag tag)
        {
            if (!ModelState.IsValid)
            {
                return View(tag);
            }

            var existingTag = await GetTagById(id);
            if (existingTag == null)
            {
                return NotFound();
            }

            // Update the properties of the fetched tag
            existingTag.Name = tag.Name;

            try
            {
                _context.Update(existingTag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TagExists(tag.TagId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // GET: Tags/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            var tag = await GetTagById(id);
            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tag = await GetTagById(id);
            if (tag == null)
            {
                return NotFound();
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Tag> GetTagById(int? id)
        {
            if (id == null)
            {
                return null; // Return null immediately if the ID is null
            }

            return await _context.Tags.FindAsync(id);
        }

        private async Task<bool> TagExists(int id)
        {
            return await _context.Tags.AnyAsync(e => e.TagId == id);
        }
    }
}
