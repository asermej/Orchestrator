using HireologyTestAts.Domain;
using HireologyTestAts.Api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using HireologyTestAts.Api.Auth;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5001);
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

            var errorResponse = new HireologyTestAts.Api.Common.ErrorResponse
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
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Test ATS API",
        Version = "v1",
        Description = "Test applicant tracking system for Orchestrator integration testing"
    });

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

// Auth0 JWT Authentication
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];
bool auth0Configured = !string.IsNullOrEmpty(auth0Domain)
    && !string.IsNullOrEmpty(auth0Audience)
    && !auth0Domain.Contains("your-tenant");

if (auth0Configured)
{
    var domain = $"https://{auth0Domain}/";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = domain;
            options.Audience = auth0Audience;

            options.MetadataAddress = $"{domain}.well-known/openid-configuration";

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "sub",
                ValidateIssuer = true,
                ValidIssuer = domain,
                ValidateAudience = true,
                ValidAudience = auth0Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5),
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
    builder.Services.AddAuthorization();
}
else
{
    // No Auth0 config: let all requests through so the app runs until Auth0 is configured
    builder.Services.AddAuthentication(DevSkipAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevSkipAuthHandler>(DevSkipAuthHandler.SchemeName, _ => { });
    builder.Services.AddAuthorization();
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTestAtsWeb", policy =>
    {
        policy.WithOrigins("http://localhost:3001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<DomainFacade>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Middleware pipeline (aligned with Orchestrator)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePlatformExceptionHandling();

// Add clean request logging middleware (development only) - AFTER exception handling
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("dev", StringComparison.OrdinalIgnoreCase))
{
    app.Use(async (context, next) =>
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;

        await next();
        stopwatch.Stop();

        var statusCode = context.Response.StatusCode;
        var emoji = statusCode switch
        {
            >= 200 and < 300 => "✓",
            >= 400 and < 500 => "⚠",
            >= 500 => "✗",
            _ => "•"
        };

        Console.WriteLine($"{emoji} [{method}] {path} → {statusCode} ({stopwatch.ElapsedMilliseconds}ms)");
    });
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowTestAtsWeb");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoint (no auth)
app.MapGet("/api/health", (IConfiguration config) =>
{
    var hasDb = !string.IsNullOrWhiteSpace(config["ConnectionStrings:HireologyTestAts"]);
    var hasOrchestratorUrl = !string.IsNullOrWhiteSpace(config["HireologyAts:BaseUrl"]);
    var hasOrchestratorKey = !string.IsNullOrWhiteSpace(config["HireologyAts:ApiKey"]);
    return Results.Ok(new
    {
        status = "healthy",
        app = "HireologyTestAts.Api",
        version = "1.0.0",
        config = new
        {
            hasDatabase = hasDb,
            hasOrchestratorUrl = hasOrchestratorUrl,
            hasOrchestratorApiKey = hasOrchestratorKey
        }
    });
});

app.Run();
