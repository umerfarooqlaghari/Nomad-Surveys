using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nomad.Api.Authorization;
using Nomad.Api.Data;
using Nomad.Api.Entities;
using Nomad.Api.Mappings;
using Nomad.Api.Middleware;
using Nomad.Api.Services;
using Nomad.Api.Services.Interfaces;
using Nomad.Api.Services.Background; // Added for background service
using System.Text;
using Nomad.Api.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        // Use PascalCase for property names to match DTOs
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // This ensures PascalCase
    });

// Add HttpContextAccessor for tenant resolution
builder.Services.AddHttpContextAccessor();

// Add MemoryCache for caching
builder.Services.AddMemoryCache();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configure Npgsql Data Source with dynamic JSON support
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Use NpgsqlConnectionStringBuilder to safely set robust timeout and keepalive settings
var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
npgsqlBuilder.Timeout = 60;
npgsqlBuilder.CommandTimeout = 60;
npgsqlBuilder["Keepalive"] = 30; // One word, no space

var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(npgsqlBuilder.ConnectionString);
// Enable dynamic JSON serialization for JSONB columns with complex types
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

// Add Entity Framework with configured data source
builder.Services.AddDbContext<NomadSurveysDbContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions => 
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10, // Increased retries
            maxRetryDelay: TimeSpan.FromSeconds(5), // Faster retries
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60); // 60 seconds timeout
    }));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, TenantRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<NomadSurveysDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization with custom policies
builder.Services.AddAuthorization(AuthorizationPolicies.AddPolicies);

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Configure Email Settings
builder.Services.Configure<Nomad.Api.Configuration.EmailSettings>(
    builder.Configuration.GetSection("Email"));

// Add application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IEvaluatorService, EvaluatorService>();
builder.Services.AddScoped<ISubjectEvaluatorService, SubjectEvaluatorService>();
builder.Services.AddScoped<IRelationshipService, RelationshipService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<ISurveyAssignmentService, SurveyAssignmentService>();
builder.Services.AddScoped<ITenantSettingsService, TenantSettingsService>();
builder.Services.AddScoped<IEmailAuditService, EmailAuditService>();

// Add cluster, competency, question services
builder.Services.AddScoped<IClusterService, ClusterService>();
builder.Services.AddScoped<ICompetencyService, CompetencyService>();
builder.Services.AddScoped<IClusterSeedingService, ClusterSeedingService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IParticipantService, ParticipantService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IReportTemplateService, ReportTemplateService>();
builder.Services.AddScoped<IReportTemplateSettingsService, ReportTemplateSettingsService>();
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<Nomad.Api.Repository.ReportAnalyticsRepository>();
builder.Services.AddScoped<IExcelReportService, ExcelReportService>();
builder.Services.AddScoped<IExcelReportRepository, ExcelReportRepository>(); // Added registration for ExcelReportRepository
builder.Services.AddScoped<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddHostedService<ReminderBackgroundService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow all origins in development
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Restrict origins in production
            var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
                ?? new[] {"https://nomadvirtual.com", 
                        "https://www.nomadvirtual.com", 
                        "https://nomad-surveys.vercel.app", 
                        "https://nomadsurveys.vercel.app" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Nomad Surveys API",
        Version = "v1",
        Description = "Multi-tenant survey management API with RBAC"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed database
// Seed database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedService.SeedAsync();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to seed database. Application will continue without seeded data.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nomad Surveys API V1");
        c.RoutePrefix = "swagger";
    });
}

// Only use HTTPS redirection in development
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// Add custom middleware
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Add tenant authorization middleware after authentication
app.UseMiddleware<TenantAuthorizationMiddleware>();

app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
