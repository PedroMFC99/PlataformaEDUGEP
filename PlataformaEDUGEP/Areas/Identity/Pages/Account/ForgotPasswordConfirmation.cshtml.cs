// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlataformaEDUGEP.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Provides the page model for the confirmation view after a user has initiated a password reset process.
    /// This page confirms that a password reset link has been sent to the user's email address.
    /// </summary>
    [AllowAnonymous]
    public class ForgotPasswordConfirmation : PageModel
    {
        /// <summary>
        /// Handles the GET request to the ForgotPasswordConfirmation page.
        /// Displays the password reset email confirmation message to the user.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
