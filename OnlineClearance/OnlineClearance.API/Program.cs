using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OnlineClearance.API.Data;
using OnlineClearance.API.Helpers;

var builder = WebApplication.CreateBuilder(args);

// ─── DATABASE ────────────────────────────────────────────────────────────────
// Fixed server version avoids AutoDetect opening a live DB connection at startup
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing from appsettings.json");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connStr,
        new MySqlServerVersion(new Version(5, 7, 0)),
        mysqlOptions =>
        {
            mysqlOptions.CommandTimeout(60);
            mysqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        }
    ));

// ─── JWT AUTH ────────────────────────────────────────────────────────────────
var jwtKey      = builder.Configuration["JwtSettings:SecretKey"]  ?? "FallbackDevKeyMustBeAtLeast32CharsLong!";
var jwtIssuer   = builder.Configuration["JwtSettings:Issuer"]     ?? "OnlineClearanceAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"]   ?? "OnlineClearanceClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ─── CONTROLLERS ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ─── SERVICES ────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddHttpContextAccessor();

// ─── SWAGGER ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Online Clearance API",
        Version     = "v1",
        Description = "Student Online Clearance System API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Enter: Bearer {your token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── MIDDLEWARE ORDER (must be exactly this order) ────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Clearance API v1");
    c.RoutePrefix = "swagger";
});

// Ensure wwwroot/signatures exists so UseStaticFiles never crashes
var signaturesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "signatures");
Directory.CreateDirectory(signaturesDir);
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── SEED DATA (non-blocking background task) ─────────────────────────────────
// Runs AFTER the app starts listening. A DB failure here will NOT crash the process.
_ = Task.Run(async () =>
{
    await Task.Delay(1500);
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Database.CanConnectAsync())
        {
            Console.WriteLine("WARNING: Cannot reach MySQL server.");
            Console.WriteLine("  --> Run database_setup.sql manually in phpMyAdmin.");
            Console.WriteLine("  --> API endpoints will fail until DB is reachable.");
            return;
        }

        Console.WriteLine("Database connection OK.");

        // Only run migrations if using EF migrations (skip if you ran the SQL script manually)
        try { await db.Database.MigrateAsync(); }
        catch { /* SQL script already created tables — safe to ignore */ }

        // Seed statuses
        if (!db.StatusTable.Any())
        {
            db.StatusTable.AddRange(
                new OnlineClearance.API.Models.StatusTable { Label = "Pending"  },
                new OnlineClearance.API.Models.StatusTable { Label = "Cleared"  },
                new OnlineClearance.API.Models.StatusTable { Label = "Rejected" }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("Status table seeded.");
        }

        // Seed admin
        if (!db.Users.Any(u => u.Username == "admin"))
        {
            db.Users.Add(new OnlineClearance.API.Models.User
            {
                Username  = "admin",
                Password  = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                FirstName = "System",
                LastName  = "Administrator",
                Role      = "admin",
                IsActive  = true
            });
            await db.SaveChangesAsync();
            Console.WriteLine("Admin user created: admin / Admin@1234");
        }
        else
        {
            Console.WriteLine("Admin user already exists.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Startup seed error: {ex.Message}");
    }
});

Console.WriteLine("Online Clearance API is starting...");
Console.WriteLine("Swagger: http://localhost:5000/swagger");

app.Run();
