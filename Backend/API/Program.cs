using API.BackgroundServices;
using API.Helpers;
using API.Setup;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Service registration
builder.Services.AddAppDatabase(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<GameTimeoutBackgroundService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AppNameService>();
builder.Services.AddAppControllers();
builder.Services.AddForwardedHeaders();
builder.Services.AddAppCors(builder.Configuration);
builder.Services.AddAppSignalR();
builder.Services.AddAppApiVersioning();
builder.Services.AddAppSwagger();
builder.Services.AddAppLocalization(builder.Configuration);

// Build and configure pipeline
var app = builder.Build();

app.SetupAppData();
app.UseAppMiddleware();
app.UseAppSwagger();
app.MapAppEndpoints();

app.Run();

// this is needed for unit testing
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}
