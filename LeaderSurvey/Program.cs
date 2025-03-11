using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configure the DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.SetPostgresVersion(9, 6)
    ));

// Add Swagger/OpenAPI (Optional, but useful for testing APIs)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // TODO: REMOVE IN PRODUCTION - Development only admin access
    app.Use(async (context, next) =>
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "TestAdmin"),
            new Claim(ClaimTypes.NameIdentifier, "test-admin-id")
        }, "TestAuthType");

        context.User = new ClaimsPrincipal(identity);
        await next();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve static files (CSS, JS, etc.)
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages(); // Map Razor Pages endpoints

app.Run();
