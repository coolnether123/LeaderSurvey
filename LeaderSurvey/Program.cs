using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Configure the DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
static void SeedData(IServiceProvider services)
{
    using var context = services.GetRequiredService<ApplicationDbContext>();
    if (context.Leaders.Any())
    {
        return; // Data already seeded
    }
    
    // Seed Leaders with required Area field
    var frontLeader = new Leader { Name = "Alice Smith", Area = "Front" };
    var kitchenLeader = new Leader { Name = "Bob Johnson", Area = "Kitchen" };
    var driveLeader = new Leader { Name = "Charlie Williams", Area = "Drive" };
    
    context.Leaders.AddRange(frontLeader, kitchenLeader, driveLeader);
    
    // Seed Surveys with Area fields and UTC dates
    var surveys = new[]
    {
        new Survey 
        { 
            Name = "Front Survey",
            Description = "Survey for Front area",
            Area = "Front",
            CreatedDate = DateTime.UtcNow,
            Leader = frontLeader
        },
        new Survey 
        { 
            Name = "Kitchen Survey",
            Description = "Survey for Kitchen area",
            Area = "Kitchen",
            CreatedDate = DateTime.UtcNow,
            Leader = kitchenLeader
        }
    };

    // If you need to set MonthYear or CompletedDate, use the helper methods
    foreach (var survey in surveys)
    {
        // Set MonthYear to first day of current month in UTC
        survey.MonthYear = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        survey.CompletedDate = DateTime.UtcNow;
    }

    context.Surveys.AddRange(surveys);
    context.SaveChanges();
}
