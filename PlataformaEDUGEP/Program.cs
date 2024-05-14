using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using PlataformaEDUGEP;
using PlataformaEDUGEP.Data;
using PlataformaEDUGEP.Models;
using PlataformaEDUGEP.Services;
using PlataformaEDUGEP.Utilities;
using WebPWrecover.Services;

// Initialize a new instance of the WebApplication builder with the program's command-line arguments.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configures the database context to use a SQL Server database.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configures the identity system for handling user authentication and authorization.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<CustomIdentityErrorDescriber>();

// Adds MVC services to the service container with support for controllers and views.
builder.Services.AddControllersWithViews();

// Dependency injection for audit services.
builder.Services.AddScoped<IFolderAuditService, FolderAuditService>();
builder.Services.AddScoped<IFileAuditService, FileAuditService>();

// Configure identity options for user lockout settings.
builder.Services.Configure<IdentityOptions>(opts => {
    opts.Lockout.AllowedForNewUsers = true;
    opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
    opts.Lockout.MaxFailedAccessAttempts = 3;
});

// Configures the email sender service using SendGrid.
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration);

// Builds the application.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error404");
    app.UseHsts();
}

// Custom error handling for 404 status codes.
app.UseStatusCodePagesWithReExecute("/Home/Error404");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configures route mapping for MVC controllers.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Execute configuration tasks on application startup.
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

// Access IConfiguration from the service provider
var configuration = services.GetRequiredService<IConfiguration>();

// Initialize roles and admin user on application startup.
await Configurations.CreateStartingRoles(services, configuration);

// Starts the application.
app.Run();
