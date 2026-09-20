using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NGODonationSystem.Data;
using NGODonationSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Add MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    string[] roles = { "Admin", "NGO", "Donor" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Seed Admin user
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Read Admin credentials from configuration (User Secrets)
    var adminEmail = configuration["Admin:Email"];
    var adminPassword = configuration["Admin:Password"];

    // Validate that Admin credentials are configured
    if (string.IsNullOrWhiteSpace(adminEmail))
    {
        throw new InvalidOperationException("Admin:Email is not configured. Please set it using User Secrets: dotnet user-secrets set \"Admin:Email\" \"your-email@example.com\"");
    }

    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException("Admin:Password is not configured. Please set it using User Secrets: dotnet user-secrets set \"Admin:Password\" \"your-secure-password\"");
    }

    // Check if Admin user already exists
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        // Create new Admin user
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "CareFund Admin",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            // Assign Admin role
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            throw new InvalidOperationException($"Failed to create Admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();