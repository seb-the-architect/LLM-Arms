using Navi_Hub.Models;
using Navi_Hub.Routes;

// Initialize ASP.NET with Hub singleton
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Hub>();
var app = builder.Build();

app.MapHealthRoutes();
app.MapNodeRoutes();

app.Run();
