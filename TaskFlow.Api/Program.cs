using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TaskFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // useful for minimal APIs and Swagger discovery
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
});

// Register SQLite DB context BEFORE calling Build()
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite("Data Source=tasks.db"));

var app = builder.Build();

// Apply EF migrations (optional but recommended for persistent DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    db.Database.Migrate(); // requires migrations or will create schema if migrations exist
}

// Enable Swagger UI in Development (recommended)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
        c.RoutePrefix = string.Empty; // serve at root
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
