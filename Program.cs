using EsotericStore.API.Data;
using EsotericStore.API.Extensions;
using EsotericStore.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ─── Puerto dinámico (Render usa PORT, default 8080) ──────────
builder.WebHost.UseUrls(
    $"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}"
);

// ─── Servicios ────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Permite uploads de hasta 10 MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

// PostgreSQL + EF Core — Supabase Pooler (Supavisor) vía IPv4
var rawConnectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró una cadena de conexión válida.");

static string EnsureParam(string cs, string key, string value)
    => cs.Contains(key, StringComparison.OrdinalIgnoreCase) ? cs : $"{cs};{key}={value}";

var connectionString = rawConnectionString;
connectionString = EnsureParam(connectionString, "Ssl Mode",                "Require");
connectionString = EnsureParam(connectionString, "Trust Server Certificate", "true");
connectionString = EnsureParam(connectionString, "Pooling",                  "false");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)));

// Servicios del negocio
builder.Services.AddApplicationServices();

// ─── JWT Authentication ────────────────────────────────────────
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? "dev-secret-cambia-esto-en-produccion-32chars!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ClockSkew                = TimeSpan.Zero
        };
    });

// CORS — orígenes locales + producción (Netlify + Render via FRONTEND_URL)
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");

var corsOrigins = new List<string>
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:5175",
    "http://localhost:5176",
    "http://localhost:5177",
    "http://localhost:3000",
    "http://localhost:4173",
    "https://transcendent-torrone-b7c5f8.netlify.app"
};

if (!string.IsNullOrWhiteSpace(frontendUrl))
    corsOrigins.Add(frontendUrl);

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueDevServer", policy =>
    {
        policy
            .WithOrigins(corsOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger con soporte Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Esoteric Store API",
        Version     = "v1",
        Description = "API para la tienda de ropa esotérica"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In          = ParameterLocation.Header,
        Description = "Ingresa el token JWT con el prefijo Bearer. Ejemplo: Bearer {token}",
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT"
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

// ─── Pipeline ────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrar la base de datos al iniciar
// En producción (DATABASE_URL presente) cualquier fallo detiene el proceso para que
// Render/el orquestador lo muestre como unhealthy y no sirva tráfico sin tablas.
// En desarrollo local, solo se loguea y la app continúa.
var isProduction = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATABASE_URL"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("Aplicando migraciones...");
        await db.Database.MigrateAsync();
        Console.WriteLine("Migraciones aplicadas correctamente.");
        await DbSeeder.SeedAsync(db);
        Console.WriteLine("Seed completado.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("=== ERROR AL INICIALIZAR LA BASE DE DATOS ===");
        Console.Error.WriteLine($"Tipo    : {ex.GetType().FullName}");
        Console.Error.WriteLine($"Mensaje : {ex.Message}");
        if (ex.InnerException is not null)
            Console.Error.WriteLine($"Inner   : {ex.InnerException.Message}");

        if (isProduction)
        {
            Console.Error.WriteLine("Entorno de producción: la aplicación se detendrá para evitar arrancar sin tablas.");
            throw;
        }

        Console.Error.WriteLine("Entorno local: la aplicación continuará. Verifica tu base de datos.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Esoteric Store API v1");
    c.RoutePrefix = "swagger";
});

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("VueDevServer");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
