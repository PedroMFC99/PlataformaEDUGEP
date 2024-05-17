// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using PlataformaEDUGEP.Enums;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Manages user registration processes.
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterModel"/> class.
        /// </summary>
        /// <param name="userManager">The user manager for handling user-related operations.</param>
        /// <param name="userStore">The user store for managing user storage.</param>
        /// <param name="signInManager">The sign-in manager for handling user sign-in operations.</param>
        /// <param name="logger">The logger for logging information about user registration.</param>
        /// <param name="emailSender">The email sender for sending emails to users.</param>
        /// <param name="configuration">The configuration where special codes and other configurations are stored.</param>
        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IConfiguration configuration) // Inject IConfiguration)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _configuration = configuration; // Set the configuration
        }

        /// <summary>
        /// Represents the input model for the registration form.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Represents the URL to return to after registering, if any.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Represents the list of external authentication schemes available.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// Represents the input fields for user registration.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// FullName of the user.
            /// </summary>
            [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
            [DataType(DataType.Text)]
            [MaxLength(30, ErrorMessage = "Nome demasiado longo.")]
            [Display(Name = "Nome apresentado")]
            public string FullName { get; set; }

            /// <summary>
            /// User Type.
            /// </summary>
            [Required(ErrorMessage = "Por favor, selecione um tipo de conta.")]
            [Display(Name = "Tipo de conta")]
            public string UserType { get; set; } // "Teacher" or "Student"

            /// <summary>
            /// Special Code, needed for teachers to register in the platform.
            /// </summary>
            // [Required(ErrorMessage = "O código especial é obrigatório.")]
            [Display(Name = "Código especial")]
            public string SpecialCode { get; set; } // No longer required for all users}
            /// <summary>
            /// Email of the user.
            /// </summary>
            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress(ErrorMessage = "O Email que introduziu não é um endereço de e-mail válido.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            /// User's password.
            /// </summary>
            [Required(ErrorMessage = "A palavra-passe é obrigatória.")]
            [StringLength(100, ErrorMessage = "A {0} tem de ser pelo menos {2} e no máximo {1} caratéres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Palavra-passe")]
            public string Password { get; set; }

            /// <summary>
            /// Confirmation of the user's password (replicate).
            /// </summary>
            [Required(ErrorMessage = "A confirmação da palavra-passe é obrigatória.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar palavra-passe")]
            [Compare("Password", ErrorMessage = "A Palavra-passe e a sua confirmação têm de ser iguais.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary>
        /// Handles the GET request for the registration page, initializing necessary data.
        /// </summary>
        /// <param name="returnUrl">The URL to redirect to after registration, if any.</param>
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        /// Handles the POST request for the registration page, attempting to register a new user.
        /// </summary>
        /// <param name="returnUrl">The URL to redirect to after a successful registration, if any.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                // Check if the user is a teacher and verify the special code
                if (Input.UserType == "Teacher")
                {
                    var mySpecialCode = _configuration["SpecialCode"];
                    if (string.IsNullOrWhiteSpace(Input.SpecialCode) || Input.SpecialCode != mySpecialCode)
                    {
                        ModelState.AddModelError(string.Empty, "O código especial está incorrecto.");
                        return Page();
                    }
                }

                var user = CreateUser();

                // Set additional user properties here
                user.FullName = Input.FullName;

                // Continue with setting username and email
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // Attempt to create the user with the given password
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Role assignment logic starts here
                    string roleName = Input.UserType; // Use the UserType input to determine the role
                    var roleResult = await _userManager.AddToRoleAsync(user, roleName);
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError($"Error adding user to role: {roleName}");
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        // Optional: Handle the failure of adding a user to a role as required
                    }
                    // Role assignment logic ends here

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Ative a sua conta",
                        $"Por favor, ative a sua conta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ApplicationUser"/> type.
        /// </summary>
        /// <returns>A new instance of <see cref="ApplicationUser"/>.</returns>
        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        /// <summary>
        /// Retrieves the user email store from the user manager.
        /// </summary>
        /// <returns>The email store used by the user manager.</returns>
        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
