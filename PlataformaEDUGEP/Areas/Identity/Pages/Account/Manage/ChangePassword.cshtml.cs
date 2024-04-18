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
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Manages the password change process for logged-in users within the Identity area. This page model is part of the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API supports the Identity UI infrastructure and may change or be removed in future releases.
    /// </summary>
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        /// <summary>
        /// Constructor initializing services and utilities for handling user management and sign-in operations.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="logger">A generic interface for logging where the category is the type of the performing class.</param>
        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Represents the data model for the form on the change password page.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Message to display on the page after processing a post request.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Data model containing the fields necessary for changing a user's password.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Current password of the user, required to authorize the password change operation.
            /// </summary>
            [Required(ErrorMessage = "A palavra-passe atual é obrigatória.")]
            [DataType(DataType.Password)]
            [Display(Name = "Palavra-passe atual")]
            public string OldPassword { get; set; }

            /// <summary>
            /// New password for the user, must meet the defined strength and length requirements.
            /// </summary>
            [Required(ErrorMessage = "A nova palavra-passe é obrigatória.")]
            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caratéres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nova palavra-passe")]
            public string NewPassword { get; set; }

            /// <summary>
            /// Confirmation for the new password to prevent typing errors.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar nova palavra-passe")]
            [Compare("NewPassword", ErrorMessage = "As palavra-passes que inseriu têm de ser iguais.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary>
        /// Loads the page for changing password. Verifies if the user has a password set and can change it.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToPage("./SetPassword");
            }

            return Page();
        }

        /// <summary>
        /// Processes the change password request. Validates the current password, sets the new password, and signs the user in again.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToPage();
        }
    }
}
