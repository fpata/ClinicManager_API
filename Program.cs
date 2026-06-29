using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using ClinicManager.DAL;
using ClinicManager.Services;
using Serilog;
using ClinicManager;

var builder = WebApplication.CreateBuilder(args);


// Configure Serilog
builder.Host.UseSerilog((context, configuration) => 
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<ISmsService, SmsService>();
builder.Services.AddTransient<IPrescriptionService, PrescriptionService>();
builder.Services.AddTransient<IWhatsAppService, WhatsAppService>();

builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.PropertyNamingPolicy = null;
         // Prevent JSON serialization errors caused by circular object graphs
         options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
         // Optionally increase max depth if your object graph is deep
         options.JsonSerializerOptions.MaxDepth = 8;
         options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
     });

//builder.Services.AddMemoryCache();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "ClinicManager API",
        Version = "v1"
    });

    // Add JWT Bearer security definition
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    // Add JWT Bearer security requirement
    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// Add EF Core DbContext dynamically based on ITenantService and DatabaseProvider setting
builder.Services.AddDbContext<ClinicDbContext>((serviceProvider, options) =>
{
    var tenantService = serviceProvider.GetRequiredService<ITenantService>();
    var connectionString = tenantService.GetTenantConnectionString();
    
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var provider = configuration["DatabaseProvider"] ?? "MySql";

    if (provider.Equals("SqlServer", System.StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(
            connectionString,
            sqlServerOptions => sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        );
    }
    else
    {
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)),
            mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        );
    }
});



var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Contains("_PLACEHOLDER"))
{
    jwtKey = "KeyForClinicManagerJWTTokenForEncryptionPassKey";
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClinicManagerIssuer";
var jwtaudience = builder.Configuration["Jwt:Audience"] ?? "ClinicManagerAudience";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = false,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Add CORS policy to allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Global exception handling middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    try
    {
        await next();
    }   
    catch (Exception ex)
    {
        logger.LogError(ex, "An unhandled exception occurred while processing the request.");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var errorResponse = new
        {
            Message = ex.Message,
            InnerException = ex.InnerException?.Message,
            StackTrace = ex.StackTrace
        };
        
        var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthentication(); // <-- Add this before UseAuthorization
app.UseAuthorization();
app.UseCors("AllowAll"); // Use CORS before authentication/authorization

app.MapControllers();

app.Run();
