// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Manages the display of generated recovery codes for users in an ASP.NET Core Identity system.
    /// This page model supports the Identity UI infrastructure and is not intended to be used directly from your code.
    /// This API may change or be removed in future releases.
    /// </summary>
    public class ShowRecoveryCodesModel : PageModel
    {
        /// <summary>
        /// Gets or sets the recovery codes to be displayed to the user.
        /// </summary>
        [TempData]
        public string[] RecoveryCodes { get; set; }

        /// <summary>
        /// Gets or sets the status message to display in the user interface.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Handles the get request to display recovery codes. If no recovery codes are available, it redirects to the Two-Factor Authentication page.
        /// </summary>
        /// <returns>The PageResult for displaying recovery codes or a RedirectToPageResult to the Two-Factor Authentication page if no codes are available.</returns>
        public IActionResult OnGet()
        {
            if (RecoveryCodes == null || RecoveryCodes.Length == 0)
            {
                return RedirectToPage("./TwoFactorAuthentication");
            }

            return Page();
        }
    }
}
