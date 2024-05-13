// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Manages the email settings of a logged-in user within the Identity area.
    /// This page model is part of the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API supports the Identity UI infrastructure and may change or be removed in future releases.
    /// </summary>
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Constructor initializing services and utilities for handling user management and email operations.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="emailSender">Service for sending emails.</param>
        public EmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Email of the user. Loaded from user manager and displayed in the form.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Indicates whether the user's email is confirmed.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        /// Status message communicated to the page after operations.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Input model for the page. Represents the email to be managed or modified.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Represents the email to be managed or modified.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// The new email to be set for the user, if they decide to change it.
            /// </summary>
            [Required(ErrorMessage = "O novo email é obrigatório.")]
            [EmailAddress(ErrorMessage = "O email não é um endereço de e-mail válido.")]
            [Display(Name = "Novo E-mail")]
            public string NewEmail { get; set; }
        }

        /// <summary>
        /// Loads the current user's email information.
        /// </summary>
        /// <param name="user">The user whose information is to be loaded.</param>
        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        /// <summary>
        /// Handler for the GET request to the page.
        /// Loads the user's email information if the user is authenticated.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        /// <summary>
        /// Handler for the POST request to change the user's email.
        /// Validates the model state, checks the new email, and sends a confirmation link if the change is initiated.
        /// </summary>
        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = Input.NewEmail, code = code },
                    protocol: Request.Scheme);
                await _emailSender.SendEmailAsync(
                    Input.NewEmail,
                    "Confirmar o seu email",
                    $"Por favor confirme o seu email <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");

                StatusMessage = "Link de ativação para mudar de email foi enviado. Por favor, verifique o seu email.";
                return RedirectToPage();
            }

            StatusMessage = "O seu email está inalterado.";
            return RedirectToPage();
        }

        /// <summary>
        /// Handler for the POST request to resend the email verification.
        /// Validates the model state, confirms the email is set, and sends a verification email.
        /// </summary>
        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                email,
                "Confirm your email",
                $"Por favor, confirme a sua conta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");

            StatusMessage = "Email de verificação enviado. Por favor, verifique o seu email";
            return RedirectToPage();
        }
    }
}
