// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
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
    /// Manages the password reset process for users. This page model is used within the ASP.NET Core Identity default UI infrastructure.
    /// This API supports the Identity UI infrastructure and is not intended to be used directly from your code.
    /// This API may change or be removed in future releases.
    /// </summary>
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initializes a new instance of <see cref="ResetPasswordModel"/>.
        /// </summary>
        /// <param name="userManager">The UserManager for managing users in a persistence store.</param>
        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Holds the input data for the form.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Represents the input model for the form to reset a user's password.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// The email address associated with the user account.
            /// This property must be a valid email address.
            /// </summary>
            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress(ErrorMessage = "O email não é um endereço de e-mail válido.")]
            public string Email { get; set; }

            /// <summary>
            /// The new password for the user account.
            /// </summary>
            [Required]
            [StringLength(20, ErrorMessage = "A palavra-passe deve ter no mínimo {2} e no máximo {1} carácteres de comprimento.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            /// Confirmation of the new password.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "A palavra-passe e a sua confirmação devem ser iguais.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            /// A token to verify the user is authorized to reset their password.
            /// </summary>
            [Required]
            public string Code { get; set; }

        }

        /// <summary>
        /// Handles the GET request to the page. Initializes the form with the reset code if present.
        /// </summary>
        /// <param name="code">The reset code needed to verify the password reset request.</param>
        /// <returns>A page result allowing the user to reset their password if the code is valid; otherwise, a bad request.</returns>
        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
                };
                return Page();
            }
        }

        /// <summary>
        /// Handles the POST request to reset a user's password.
        /// </summary>
        /// <returns>A redirect to the password reset confirmation page if successful, otherwise a page displaying validation errors.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}
