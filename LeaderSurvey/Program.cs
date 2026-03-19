using LeaderSurvey.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Security.Claims;

// Ensure Npgsql uses modern timestamp behavior globally.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configure the DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Correct configuration for latest Npgsql on .NET 8
    options.UseNpgsql(connectionString, o =>
    {
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Listen on the port specified by the PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var app = builder.Build();

// ---- MIGRATION/SEED JOB MODE ----
if (Environment.GetEnvironmentVariable("MIGRATE_ONLY") == "true")
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    Console.WriteLine("Applying EF Core migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations complete. Seeding...");
    await DbInitializer.InitializeAsync(db);  // safe to call here
    Console.WriteLine("Seeding complete. Exiting.");
    return; // don't start Kestrel
}

// ---- NORMAL WEB STARTUP (non-blocking, no crashes) ----
var ready = true; // flip this if you add other long inits
app.MapGet("/healthz", () => ready ? Results.Ok("ok") : Results.StatusCode(503));

// OPTIONAL: do seeding in background if you really need it at runtime
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await DbInitializer.InitializeAsync(db);  // idempotent seed recommended
            Console.WriteLine("Background seed complete.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Background seed failed: {ex}");
            // don't crash the container
        }
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();