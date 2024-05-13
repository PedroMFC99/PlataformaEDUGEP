// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Handles the request to resend an email confirmation. This page model is used within the ASP.NET Core Identity default UI infrastructure.
    /// This API supports the Identity UI infrastructure and is not intended to be used directly from your code.
    /// This API may change or be removed in future releases.
    /// </summary>
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Initializes a new instance of <see cref="ResendEmailConfirmationModel"/>.
        /// </summary>
        /// <param name="userManager">The UserManager for managing users in a persistence store.</param>
        /// <param name="emailSender">The service used to send emails.</param>
        public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Holds the input data for the form.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Represents the input model for the form to resend an email confirmation.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// The email address to resend the confirmation email to.
            /// This property must be a valid email address.
            /// </summary>
            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress(ErrorMessage = "O email não é um endereço de e-mail válido.")]
            public string Email { get; set; }
        }

        /// <summary>
        /// Handles the GET request to the page.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Handles the POST request to resend the email confirmation.
        /// If the email is valid and the user exists, it will generate a new confirmation token and send an email.
        /// </summary>
        /// <returns>The page result after processing the post request, either displaying errors or confirming the email resend.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Verificação de email enviada. Por favor, verifique o seu email.");
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = userId, code = code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                Input.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
            return Page();
        }
    }
}
