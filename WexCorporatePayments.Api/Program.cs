using Microsoft.EntityFrameworkCore;
using WexCorporatePayments.Application;
using WexCorporatePayments.Infrastructure;
using WexCorporatePayments.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "WexCorporatePayments API", 
        Version = "v1",
        Description = "API for managing corporate purchase transactions and currency conversion using Treasury API"
    });
});

// Register Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure ProblemDetails
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations automatically on startup (only for relational databases)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
