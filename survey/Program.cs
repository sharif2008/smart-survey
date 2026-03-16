using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;
using SurveyApi.Data;
using SurveyApi.Helpers;
using SurveyApi.Models;
using SurveyApi.Options;
using SurveyApi.Services;
using SurveyApi.Services.Implementations;
using SurveyApi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var logPath = builder.Configuration["Serilog:LogPath"] ?? "Logs/api-.log";
var errorLogPath = builder.Configuration["Serilog:ErrorLogPath"] ?? "Logs/errors-.log";

builder.Host.UseSerilog((_, config) => config
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        errorLogPath,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// --- Services ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri) return false;
                // Allow localhost / 127.0.0.1 on any port (Angular dev server, etc.)
                if (uri.Scheme != "http" && uri.Scheme != "https") return false;
                return uri.Host is "localhost" or "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Survey API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(9, 0, 0)));
    else
        options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<ISurveyPageService, SurveyPageService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IExportService, ExportService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "DefaultKeyAtLeast32CharactersLong!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SurveyApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SurveyApi";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// --- Pipeline ---
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseMiddleware<SubmissionRateLimitMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- Seed (roles + default admin) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var dbProvider = config["DatabaseProvider"] ?? "SqlServer";

    // MySQL: ensure Id columns have AUTO_INCREMENT (fixes "Field 'Id' doesn't have a default value")
    if (dbProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        var alterCommands = new[]
        {
            "ALTER TABLE `Roles` MODIFY COLUMN `Id` INT NOT NULL AUTO_INCREMENT",
            "ALTER TABLE `Users` MODIFY COLUMN `Id` INT NOT NULL AUTO_INCREMENT",
            "ALTER TABLE `Surveys` MODIFY COLUMN `Id` INT NOT NULL AUTO_INCREMENT",
            "ALTER TABLE `Questions` MODIFY COLUMN `Id` INT NOT NULL AUTO_INCREMENT",
            "ALTER TABLE `SurveyResponses` MODIFY COLUMN `Id` INT NOT NULL AUTO_INCREMENT",
            "ALTER TABLE `Answers` MODIFY COLUMN `Id` INT NOT NULL AUTO_INCREMENT"
        };
        foreach (var sql in alterCommands)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
            }
            catch
            {
                // Table may not exist yet or column already has AUTO_INCREMENT
            }
        }
    }

    if (!await db.Roles.AnyAsync().ConfigureAwait(false))
    {
        db.Roles.AddRange(new Role { Name = "Admin" }, new Role { Name = "Researcher" }, new Role { Name = "Participant" });
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin").ConfigureAwait(false);
    if (adminRole != null && !await db.Users.AnyAsync(u => u.RoleId == adminRole.Id).ConfigureAwait(false))
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        db.Users.Add(new User
        {
            FullName = "Admin User",
            Email = "admin@survey.local",
            PasswordHash = hasher.HashPassword(null!, "Admin@123"),
            RoleId = adminRole.Id
        });
        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
