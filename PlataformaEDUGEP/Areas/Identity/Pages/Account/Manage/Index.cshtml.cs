// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlataformaEDUGEP.Models;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Manages the user profile information in an ASP.NET Core Identity system.
    /// This page model supports the Identity UI infrastructure and is not intended to be used directly from your code.
    /// This API may change or be removed in future releases.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        /// <summary>
        /// Initializes a new instance of <see cref="IndexModel"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing users in a persistence store.</param>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Gets or sets the user's username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Represents the data needed from the user to manage their profile.
        /// </summary>
        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            [RegularExpression(@"^\d{9}$", ErrorMessage = "Por favor, insira um número de telefone com exatamente nove dígitos.")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Introduza o seu nome.")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            public string AboutMe { get; set; } 
        }

        /// <summary>
        /// Loads user data into the model for display.
        /// </summary>
        /// <param name="user">The user whose information is to be loaded.</param>
        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var userFullName = user.FullName; // Assuming you have a FullName property in ApplicationUser
            var userAboutMe = user.AboutMe;

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FullName = userFullName, // Set the FullName
                AboutMe = userAboutMe
            };
        }

        /// <summary>
        /// Loads the user's profile data asynchronously when the page is accessed.
        /// </summary>
        /// <returns>The page result.</returns>
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
        /// Updates the user's profile data based on the form submission.
        /// </summary>
        /// <returns>The page result.</returns>
        public async Task<IActionResult> OnPostAsync()
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

            // Update FullName
            if (Input.FullName != user.FullName)
            {
                user.FullName = Input.FullName; // Update the user's FullName
                var setFullNameResult = await _userManager.UpdateAsync(user);
                if (!setFullNameResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set full name.";
                    return RedirectToPage();
                }
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            if (Input.AboutMe != user.AboutMe)
            {
                user.AboutMe = Input.AboutMe; // Update the user's AboutMe section
                var setAboutMeResult = await _userManager.UpdateAsync(user);
                if (!setAboutMeResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set about me.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "O seu perfil foi atualizado";
            return RedirectToPage();
        }
    }
}
