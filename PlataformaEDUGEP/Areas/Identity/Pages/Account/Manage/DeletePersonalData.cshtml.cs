// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Manages the deletion of personal data for the logged-in user within the Identity area. This page model is part of the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API supports the Identity UI infrastructure and may change or be removed in future releases.
    /// </summary>
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

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Palavra-passe incorreta.");
                    return Page();
                }
            }

            // Set UserId to null in the Folder table
            var folders = _context.Folder.Where(f => f.User.Id == user.Id);
            foreach (var folder in folders)
            {
                folder.User = null;
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

            return Redirect("~/");
        }
    }
}
