using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using POS_Shoes;
using POS_Shoes.Implementations.Services;
using POS_Shoes.Models.Data;
using POS_Shoes.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddSingleton(provider =>
{
    var config = builder.Configuration.GetSection("CloudinarySettings");
    var account = new Account(
        config["CloudName"],
        config["ApiKey"],
        config["ApiSecret"]
    );
    return new Cloudinary(account);
});

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();


// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "POS_Shoes_Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add Authorization with Role-based Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Manager"));
    options.AddPolicy("RequireSaler", policy => policy.RequireRole("Saler"));
    options.AddPolicy("RequireAccountant", policy => policy.RequireRole("Accountant"));
    options.AddPolicy("RequireMarketing", policy => policy.RequireRole("Marketing"));

    // Complex policies
    options.AddPolicy("RequireManagerOrAccountant", policy =>
        policy.RequireRole("Manager", "Accountant"));
    options.AddPolicy("RequireSalesTeam", policy =>
        policy.RequireRole("Manager", "Saler"));
});

// Add Logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 1. HTTPS Redirection (đầu tiên)
app.UseHttpsRedirection();

// 2. Static Files
app.UseStaticFiles();

// 3. Method Override Middleware (QUAN TRỌNG: Trước UseRouting)
app.Use(async (context, next) =>
{
    if (context.Request.Method == "POST" &&
        context.Request.HasFormContentType &&
        context.Request.Form.ContainsKey("_method"))
    {
        var method = context.Request.Form["_method"].ToString().ToUpperInvariant();
        if (method == "PUT" || method == "PATCH" || method == "DELETE")
        {
            context.Request.Method = method;
        }
    }
    await next();
});

// 4. Routing
app.UseRouting();

// 5. Authentication & Authorization (SAU UseRouting)
app.UseAuthentication();
app.UseAuthorization();

// 6. Map Static Assets (nếu sử dụng .NET 8+)
if (app.Environment.IsDevelopment())
{
    app.MapStaticAssets();
}

// 7. Configure routing - Areas FIRST, then default
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
