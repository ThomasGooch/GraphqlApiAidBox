var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Load secrets from appsettings or user secrets
var configuration = builder.Configuration;
var env = builder.Environment;
configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .AddUserSecrets<Program>(optional: true)
             .AddEnvironmentVariables();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
