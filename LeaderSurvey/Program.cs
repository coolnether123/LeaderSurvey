using LeaderSurvey.Data;
using LeaderSurvey.Models;
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

// Add Swagger/OpenAPI (Optional, but useful for testing APIs)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Seed data (only in development)
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        SeedData(services);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve static files (CSS, JS, etc.)
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages(); // Map Razor Pages endpoints

app.Run();

// Seed Data Method (FOR DEVELOPMENT ONLY)
void SeedData(IServiceProvider services)
{
    using var context = services.GetRequiredService<ApplicationDbContext>();

    // Check if data already exists
    if (context.Leaders.Any()) return;

    // Add sample leaders
    var leaders = new[]
    {
        new Leader { Name = "John Smith", Area = "Engineering" },
        new Leader { Name = "Jane Doe", Area = "Marketing" },
        new Leader { Name = "Bob Johnson", Area = "Sales" }
    };
    context.Leaders.AddRange(leaders);
    context.SaveChanges();

    // Add sample surveys with explicit UTC dates
    var surveys = new[]
    {
        new Survey
        {
            Name = "Engineering Survey 2024",
            Description = "Annual engineering team survey",
            Area = "Engineering",
            LeaderId = leaders[0].Id,
            Date = DateTime.UtcNow,  // Already UTC
            MonthYear = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = "Active"
        },
        new Survey
        {
            Name = "Marketing Goals Review",
            Description = "Quarterly marketing review",
            Area = "Marketing",
            LeaderId = leaders[1].Id,
            Date = DateTime.UtcNow,  // Already UTC
            MonthYear = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = "Pending"
        }
    };
    context.Surveys.AddRange(surveys);
    context.SaveChanges();
}
