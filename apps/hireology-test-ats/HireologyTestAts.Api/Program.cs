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

var auth0Authority = builder.Configuration["Auth0:Authority"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddScoped<HireologyTestAts.Api.Services.JobsRepository>();
builder.Services.AddScoped<HireologyTestAts.Api.Services.GroupsRepository>();
builder.Services.AddScoped<HireologyTestAts.Api.Services.OrganizationsRepository>();
builder.Services.AddScoped<HireologyTestAts.Api.Services.UsersRepository>();
builder.Services.AddScoped<HireologyTestAts.Api.Services.ICurrentUserService, HireologyTestAts.Api.Services.CurrentUserService>();
builder.Services.AddScoped<HireologyTestAts.Api.Services.UserSessionsRepository>();
builder.Services.AddScoped<HireologyTestAts.Api.Services.UserAccessService>();
builder.Services.AddScoped<HireologyTestAts.Api.Services.UserAccessRepository>();
builder.Services.AddHttpClient<HireologyTestAts.Api.Services.OrchestratorSyncService>();
builder.Services.AddHttpClient<HireologyTestAts.Api.Services.OrchestratorUserProvisioningService>();

bool auth0Configured = !string.IsNullOrEmpty(auth0Authority) && !string.IsNullOrEmpty(auth0Audience);

if (auth0Configured)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = auth0Authority!.TrimEnd('/') + "/";
            options.Audience = auth0Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAlgorithms = new[] { "RS256" }
            };
        });
    builder.Services.AddAuthorization();
}
else
{
    // No Auth0 config: let all requests through so the app runs until you set Auth0:Authority and Auth0:Audience
    builder.Services.AddAuthentication(DevSkipAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevSkipAuthHandler>(DevSkipAuthHandler.SchemeName, _ => { });
    builder.Services.AddAuthorization();
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3001").AllowAnyMethod().AllowAnyHeader();
    });
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
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
