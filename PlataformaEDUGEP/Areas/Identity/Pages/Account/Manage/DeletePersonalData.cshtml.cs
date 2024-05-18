using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

public class DeletePersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<DeletePersonalDataModel> _logger;
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor initializing services and utilities for handling user management and sign-in operations.
    /// </summary>
    /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="logger">A generic interface for logging where the category is the type of the performing class.</param>
    /// <param name="context">Provides the APIs for interacting with the application's database context.</param>
    public DeletePersonalDataModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<DeletePersonalDataModel> logger,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Represents the data model for the form on the delete personal data page.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; }

    /// <summary>
    /// Data model containing the fields necessary for authenticating the user before deleting personal data.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Current password of the user, required to authorize the deletion operation.
        /// </summary>
        [Required(ErrorMessage = "Introduza a sua palavra-passe.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    /// <summary>
    /// Indicates whether the user's account requires a password for deleting personal data.
    /// </summary>
    public bool RequirePassword { get; set; }

    /// <summary>
    /// Indicates whether the user is an admin.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Loads the page for deleting personal data. Verifies if the user has a password set and if so, requires it for deletion.
    /// </summary>
    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        return Page();
    }

    /// <summary>
    /// Processes the deletion of the user's personal data after confirming their password, if required.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            ModelState.AddModelError(string.Empty, "Admins are not allowed to delete their own account.");
            return Page();
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        if (RequirePassword)
        {
            if (!await _userManager.CheckPasswordAsync(user, Input.Password))
            {
                ModelState.AddModelError(string.Empty, "Palavra-passe incorreta.");
                return Page();
            }
        }

        // Update files to set LastEditorFullName to "[Utilizador desconhecido]"
        var userFiles = _context.StoredFile.Where(f => f.UserId == user.Id || f.LastEditorFullName == user.FullName).ToList();
        foreach (var file in userFiles)
        {
            if (file.LastEditorFullName == user.FullName)
            {
                file.LastEditorFullName = "[Utilizador desconhecido]";
            }
        }

        await _context.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        var userId = await _userManager.GetUserIdAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unexpected error occurred deleting user.");
        }

        await _signInManager.SignOutAsync();

        _logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

        TempData["AccountDeleted"] = "A sua conta foi apagada com sucesso.";

        return Redirect("~/");
    }
}