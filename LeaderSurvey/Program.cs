using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Configure the DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.SetPostgresVersion(9, 6)
    ));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
});

// Add Swagger/OpenAPI (Optional, but useful for testing APIs)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
Console.WriteLine("Application has started and is building the pipeline...");

// Very early logging middleware to catch all requests
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("*** Incoming Request: {Method} {Path} ***", context.Request.Method, context.Request.Path);
    await next();
});

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting application initialization...");

    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    logger.LogInformation("Services resolved successfully. Starting database initialization...");

    await DbInitializer.InitializeAsync(context, userManager, roleManager, builder.Configuration);

    logger.LogInformation("Database initialization completed.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve static files (CSS, JS, etc.)
app.UseRouting();

// Custom logging middleware for debugging authentication flow
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request Path: {Path}, IsAuthenticated (before auth middleware): {IsAuthenticated}",
        context.Request.Path, context.User.Identity.IsAuthenticated);
    await next();
});

app.UseAuthentication(); // Add authentication middleware

// Custom logging middleware for debugging authentication flow (after UseAuthentication)
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request Path: {Path}, IsAuthenticated (after auth middleware): {IsAuthenticated}",
        context.Request.Path, context.User.Identity.IsAuthenticated);
    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages(); // Map Razor Pages endpoints

app.Run();