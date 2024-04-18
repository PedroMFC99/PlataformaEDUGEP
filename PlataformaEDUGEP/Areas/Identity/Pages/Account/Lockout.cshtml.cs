// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Provides the page model for the lockout page.
    /// This page is displayed when a user has been locked out of their account due to multiple failed login attempts.
    /// This class supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used directly from your code.
    /// This API may change or be removed in future releases.
    /// </summary>
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        /// <summary>
        /// Handler for the GET request to the lockout page.
        /// Displays the lockout message to the user.
        /// This method supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used directly from your code.
        /// This API may change or be removed in future releases.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
