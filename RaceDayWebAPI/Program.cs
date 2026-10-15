using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal; // Note: This line has a typo ("Microsft" instead of "Microsoft")
using Microsoft.IdentityModel.Tokens;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Ensure Jwt key exists for dev
if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]) && builder.Environment.IsDevelopment())
{
    builder.Configuration.AddInMemoryCollection(new[]
    {
        new KeyValuePair<string, string?>("Jwt:Key", "dev-token-key-please-change-32chars")
    });
}

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Always use InMemory database for this application (no SQL Server references)
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseInMemoryDatabase("RaceDay_InMemory")); 

builder.Services.AddScoped<TokenService>();

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

// Ensure in-memory DB is created (no migrations required)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Guard app.Run so tests that reference this assembly don't start the host and block test discovery.
// Detect common test runner assemblies (xunit) and skip Run during test execution.
bool runningUnderTest = AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.GetName().Name?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) == true
           || a.GetName().Name?.StartsWith("Microsoft.VisualStudio.TestPlatform", StringComparison.OrdinalIgnoreCase) == true);

if (!runningUnderTest)
{
    app.Run();
}
