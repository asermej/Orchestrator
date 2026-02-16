using Orchestrator.Domain;
using Orchestrator.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

//Sets up logging
//Sets up dependency injection
//Loads configuration from:
//  appsettings.json
//  appsettings.{Environment}.json
//  Environment variables
//  Command-line args
//  Example use: var imdbUrl = builder.Configuration["AppSettings:ImdbServiceBaseUrl"];
var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5000
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000);
});

// Add services
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize model validation error responses to match our ErrorResponse format
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => $"{e.Key}: {string.Join(", ", e.Value!.Errors.Select(x => x.ErrorMessage))}")
                .ToList();
            
            var errorResponse = new Orchestrator.Api.Common.ErrorResponse
            {
                StatusCode = 400,
                Message = errors.Count > 0 ? string.Join("; ", errors) : "Validation failed",
                ExceptionType = "ValidationException",
                IsBusinessException = true,
                IsTechnicalException = false,
                Timestamp = DateTime.UtcNow
            };
            
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(errorResponse)
            {
                StatusCode = 400
            };
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Orchestrator API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Auth0 JWT Authentication
var domain = $"https://{builder.Configuration["Auth0:Domain"]}/";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = domain;
        options.Audience = builder.Configuration["Auth0:Audience"];
        
        // Configure metadata address explicitly to ensure proper OIDC discovery
        options.MetadataAddress = $"{domain}.well-known/openid-configuration";
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "sub", // Auth0 uses "sub" for user ID
            ValidateIssuer = true,
            ValidIssuer = domain,
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth0:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            // Required for handling both JWS and JWE tokens
            RequireSignedTokens = true
        };
        
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                return Task.CompletedTask;
            }
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNext", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<DomainFacade>();

// Add memory cache for agent voice caching
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/_diag/config", (IConfiguration config) => Results.Ok(new
    {
        HasDb = !string.IsNullOrWhiteSpace(config["ConnectionStrings:DbConnectionString"]),
        HasOpenAiKey = !string.IsNullOrWhiteSpace(config["Gateways:OpenAI:ApiKey"]),
        StorageProvider = config["Storage:Provider"],
        StorageContainer = config["Storage:BlobContainer"],
        HasBlobConn = !string.IsNullOrWhiteSpace(config["Storage:BlobConnectionString"])
    }));
}


// Note: Upload directory creation removed - images now stored in Azure Blob Storage

// Create training-data directory if it doesn't exist
var trainingDataPath = Path.Combine(Directory.GetCurrentDirectory(), "training-data", "agents");
if (!Directory.Exists(trainingDataPath))
{
    Directory.CreateDirectory(trainingDataPath);
}

// Middleware

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePlatformExceptionHandling();

// Add clean request logging middleware (development only) - AFTER exception handling
// Check for both "Development" and "dev" environments
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("dev", StringComparison.OrdinalIgnoreCase))
{
    app.Use(async (context, next) =>
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;
        
        await next();
        stopwatch.Stop();
        
        // Skip logging for noisy auth/health check endpoints
        var shouldSkipLogging = context.Request.Path.StartsWithSegments("/api/v1/User/by-auth0-sub");
        
        if (!shouldSkipLogging)
        {
            var statusCode = context.Response.StatusCode;
            var emoji = statusCode switch
            {
                >= 200 and < 300 => "✓",
                >= 400 and < 500 => "⚠",
                >= 500 => "✗",
                _ => "•"
            };
            
            // Write directly to stdout for clean logs (bypasses logging framework formatting)
            Console.WriteLine($"{emoji} [{method}] {path} → {statusCode} ({stopwatch.ElapsedMilliseconds}ms)");
        }
    });
}

// Only use HTTPS redirection in production
// In development, we typically run on HTTP only
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Apply CORS first so static files also have CORS headers
app.UseCors("AllowNext");

// Serve static files from wwwroot (images now served via /api/v1/images/{key} from blob storage)
app.UseStaticFiles();

// API key authentication for ATS endpoints (before JWT auth so it gets first chance)
app.UseApiKeyAuth();

// Candidate session JWT authentication for /api/v1/candidate/* endpoints
app.UseCandidateSessionAuth();

app.UseAuthentication();
app.UseAuthorization();

// Resolve the authenticated user's ATS group/org access context
app.UseUserContext();

app.MapControllers();

app.Run();