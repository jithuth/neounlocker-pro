using NEOUnlocker.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "NEOUnlocker Pro API",
        Version = "v1",
        Description = "Secure firmware flash session management API"
    });
});

// Register application services
builder.Services.AddSingleton<IFirmwareService, FirmwareService>();
builder.Services.AddSingleton<ISessionService, SessionService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Background task for cleaning up expired sessions
var sessionService = app.Services.GetRequiredService<ISessionService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

var cleanupTimer = new System.Threading.Timer(_ =>
{
    try
    {
        sessionService.CleanupExpiredSessions();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to cleanup expired sessions");
    }
}, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

logger.LogInformation("NEOUnlocker Pro Server starting...");

app.Run();
