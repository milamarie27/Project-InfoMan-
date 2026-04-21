using Microsoft.EntityFrameworkCore;
using OnlineClearanceSystem.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ✅ SAFE CONNECTION STRING (fix null warning)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string 'DefaultConnection' is missing in appsettings.json");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();