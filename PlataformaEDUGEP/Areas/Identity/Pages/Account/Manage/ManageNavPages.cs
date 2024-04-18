// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace  PlataformaEDUGEP.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Provides navigation functionality for managing account-related pages.
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public static class ManageNavPages
    {
        /// <summary>
        /// Gets the name of the index page.
        /// </summary>
        public static string Index => "Index";

        /// <summary>
        /// Gets the name of the email page.
        /// </summary>
        public static string Email => "Email";

        /// <summary>
        /// Gets the name of the change password page.
        /// </summary>
        public static string ChangePassword => "ChangePassword";

        /// <summary>
        /// Gets the name of the download personal data page.
        /// </summary>
        public static string DownloadPersonalData => "DownloadPersonalData";

        /// <summary>
        /// Gets the name of the delete personal data page.
        /// </summary>
        public static string DeletePersonalData => "DeletePersonalData";

        /// <summary>
        /// Gets the name of the external logins page.
        /// </summary>
        public static string ExternalLogins => "ExternalLogins";

        /// <summary>
        /// Gets the name of the personal data page.
        /// </summary>
        public static string PersonalData => "PersonalData";

        /// <summary>
        /// Gets the name of the two-factor authentication page.
        /// </summary>
        public static string TwoFactorAuthentication => "TwoFactorAuthentication";

        /// <summary>
        /// Gets the navigation class for the index page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        /// <summary>
        /// Gets the navigation class for the email page.
        /// </summary>
        public static string EmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, Email);

        /// <summary>
        /// Gets the navigation class for the change password page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        public static string ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);

        /// <summary>
        /// Gets the navigation class for the download personal data page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        public static string DownloadPersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DownloadPersonalData);

        /// <summary>
        /// Gets the navigation class for the delete personal data page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        public static string DeletePersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeletePersonalData);

        /// <summary>
        /// Gets the navigation class for the external logins page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        public static string ExternalLoginsNavClass(ViewContext viewContext) => PageNavClass(viewContext, ExternalLogins);

        /// <summary>
        /// Gets the navigation class for the personal data page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        public static string PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);

        /// <summary>
        /// Gets the navigation class for the two-factor authentication page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        public static string TwoFactorAuthenticationNavClass(ViewContext viewContext) => PageNavClass(viewContext, TwoFactorAuthentication);

        /// <summary>
        /// Gets the navigation class for a specific page.
        /// </summary>
        /// <param name="viewContext">The view context.</param>
        /// <param name="page">The name of the page.</param>
        public static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
    }
}
