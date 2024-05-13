// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Manages the confirmation of email changes for users.
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // <summary>
        /// Initializes a new instance of <see cref="ConfirmEmailChangeModel"/> using the user and sign-in managers.
        /// </summary>
        /// <param name="userManager">The user manager for handling users in a persistence store.</param>
        /// <param name="signInManager">The sign-in manager for handling user authentication sessions.</param>
        public ConfirmEmailChangeModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Gets or sets the status message indicating the result of the email change operation.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Handles the email change confirmation request.
        /// Validates the user ID, new email, and confirmation code. If successful, updates both the email and username.
        /// </summary>
        /// <param name="userId">The ID of the user whose email is to be changed.</param>
        /// <param name="email">The new email to confirm.</param>
        /// <param name="code">The confirmation code to validate the change.</param>
        /// <returns>A redirect to the Index page on failure or remains on the same page displaying a status message on success or error.</returns>
        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                StatusMessage = "Ocorreu um erro ao mudar o email.";
                return Page();
            }

            // In the UI, email and user name are one and the same, so when we update the email
            // we need to update the user name.
            var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
            if (!setUserNameResult.Succeeded)
            {
                StatusMessage = "Ocorreu um erro ao mudar o nome de utilizador.";
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Obrigado pela confirmação da mudança de email.";
            return Page();
        }
    }
}
