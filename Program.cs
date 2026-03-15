using EsotericStore.API.Data;
using EsotericStore.API.Extensions;
using EsotericStore.API.Middleware;
using Microsoft.EntityFrameworkCore;
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
// DATABASE_URL debe estar en formato ADO.NET:
//   Server=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;
//   User Id=postgres.[ID];Password=[PWD];Ssl Mode=Require;Trust Server Certificate=true;Pooling=false
// Los parámetros de SSL y Pooling se garantizan por código aunque no vengan en la variable.
var rawConnectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró una cadena de conexión válida.");

// Parámetros requeridos por Supavisor en contenedores Linux (Render).
// Se añaden sólo si no están presentes para no duplicar.
static string EnsureParam(string cs, string key, string value)
    => cs.Contains(key, StringComparison.OrdinalIgnoreCase) ? cs : $"{cs};{key}={value}";

var connectionString = rawConnectionString;
connectionString = EnsureParam(connectionString, "Ssl Mode",                "Require");
connectionString = EnsureParam(connectionString, "Trust Server Certificate", "true");
connectionString = EnsureParam(connectionString, "Pooling",                  "false");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Servicios del negocio
builder.Services.AddApplicationServices();

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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Esoteric Store API",
        Version = "v1",
        Description = "API para la tienda de ropa esotérica"
    });
});

// ─── Pipeline ────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrar la base de datos al iniciar
// try-catch para que un fallo de conexión imprima el error completo en lugar de
// terminar el proceso con el código 139 (SIGSEGV / fallo silencioso en Linux).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("=== ERROR AL INICIALIZAR LA BASE DE DATOS ===");
        Console.Error.WriteLine($"Tipo    : {ex.GetType().FullName}");
        Console.Error.WriteLine($"Mensaje : {ex.Message}");
        if (ex.InnerException is not null)
            Console.Error.WriteLine($"Inner   : {ex.InnerException.Message}");
        Console.Error.WriteLine("La aplicación continuará sin migraciones. Verifica DATABASE_URL.");
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
app.UseAuthorization();
app.MapControllers();

app.Run();