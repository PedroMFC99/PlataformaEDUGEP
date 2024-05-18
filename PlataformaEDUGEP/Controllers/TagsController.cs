using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Controllers
{
    /// <summary>
    /// Manages operations related to tags including creating, editing, deleting, and displaying tags.
    /// </summary>
    public class TagsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagsController"/> class.
        /// </summary>
        /// <param name="context">The database context used for tag management.</param>
        /// <param name="viewEngine">The view engine used to check for view existence.</param>
        public TagsController(ApplicationDbContext context, ICompositeViewEngine viewEngine)
        {
            _context = context;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// Displays the main view of tags, optionally filtered by a search string.
        /// </summary>
        /// <param name="search">The search term used to filter tags.</param>
        /// <returns>The view displaying a list of tags.</returns>
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

        /// <summary>
        /// Displays details for a specific tag.
        /// </summary>
        /// <param name="id">The ID of the tag to display.</param>
        /// <returns>A view showing tag details or NotFound if the tag does not exist.</returns>
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

        /// <summary>
        /// Provides a view to create a new tag.
        /// </summary>
        /// <returns>A view with a form to create a new tag.</returns>
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreateTagPartial");
            }
            return View();
        }


        /// <summary>
        /// Handles the creation of a new tag.
        /// </summary>
        /// <param name="tag">The tag to create.</param>
        /// <returns>Redirects to the index if successful, otherwise returns the view with errors.</returns>
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

        /// <summary>
        /// Provides a view to edit an existing tag.
        /// </summary>
        /// <param name="id">The ID of the tag to edit.</param>
        /// <returns>A view with a form to edit the tag or NotFound if the tag does not exist.</returns>
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


        /// <summary>
        /// Handles the update of an existing tag.
        /// </summary>
        /// <param name="id">The ID of the tag to update.</param>
        /// <param name="tag">The updated tag data.</param>
        /// <returns>Redirects to the index if successful, otherwise returns the view with errors.</returns>
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

        /// <summary>
        /// Displays a confirmation dialog for deleting a tag.
        /// </summary>
        /// <param name="id">The ID of the tag to delete.</param>
        /// <returns>A view asking for confirmation to delete the tag or NotFound if the tag does not exist.</returns>
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

        /// <summary>
        /// Handles the deletion of a tag after confirmation.
        /// </summary>
        /// <param name="id">The ID of the tag to delete.</param>
        /// <returns>Redirects to the index if successful.</returns>
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

        /// <summary>
        /// Retrieves a tag by its ID from the database.
        /// </summary>
        /// <param name="id">The ID of the tag to retrieve.</param>
        /// <returns>The tag if found; otherwise, null.</returns>
        private async Task<Tag> GetTagById(int? id)
        {
            if (id == null)
            {
                return null; // Return null immediately if the ID is null
            }

            return await _context.Tags.FindAsync(id);
        }

        /// <summary>
        /// Checks if a tag exists in the database.
        /// </summary>
        /// <param name="id">The ID of the tag to check.</param>
        /// <returns>True if the tag exists, false otherwise.</returns>
        private async Task<bool> TagExists(int id)
        {
            return await _context.Tags.AnyAsync(e => e.TagId == id);
        }

        /// <summary>
        /// Checks if a view with the specified name exists.
        /// </summary>
        /// <param name="name">The name of the view to check for existence.</param>
        /// <returns>A boolean value indicating whether the view exists (true) or not (false).</returns>
        private bool ViewExists(string name)
        {
            var result = _viewEngine.FindView(ControllerContext, name, false);
            return result.Success;
        }

    }
}
