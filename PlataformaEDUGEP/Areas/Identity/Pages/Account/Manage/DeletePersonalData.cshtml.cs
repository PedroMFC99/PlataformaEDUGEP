using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using System.ComponentModel.DataAnnotations;

public class DeletePersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<DeletePersonalDataModel> _logger;
    private readonly ApplicationDbContext _context;

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

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Introduza a sua palavra-passe.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public bool RequirePassword { get; set; }
    public bool IsAdmin { get; set; }

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

        // Retrieve and delete associated folders and files within those folders
        var folders = _context.Folder.Where(f => f.User.Id == user.Id).ToList();
        foreach (var folder in folders)
        {
            var filesInFolder = _context.StoredFile.Where(f => f.FolderId == folder.FolderId).ToList();
            foreach (var file in filesInFolder)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", file.StoredFileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            _context.StoredFile.RemoveRange(filesInFolder);
        }

        _context.Folder.RemoveRange(folders);

        var userFiles = _context.StoredFile.Where(f => f.UserId == user.Id).ToList();
        foreach (var file in userFiles)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", file.StoredFileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        _context.StoredFile.RemoveRange(userFiles);

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
