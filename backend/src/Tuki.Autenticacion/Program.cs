using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using Tuki.Autenticacion.Data;
using Tuki.Autenticacion.OpenApi;
using Tuki.Autenticacion.Options;
using Tuki.Autenticacion.Services;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

string Required(string key) =>
    builder.Configuration[key]
    ?? throw new InvalidOperationException(
        $"Falta {key.Replace(":", "__")}: revisa tu archivo .env."
    );

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddOpenApi(
    "publico",
    options =>
    {
        // Quita el segmento /autenticacion de las rutas para que coincidan con
        // las rutas públicas del Gateway (/api/auth, /api/users, etc.)
        options.AddDocumentTransformer<SegmentoPublicoTransformer>();

        // Sin hosts absolutos: Scalar solo calcula la URL base dinámica con rutas
        // relativas, así las pruebas por el devtunnel apuntan al túnel y no a localhost
        options.AddDocumentTransformer(
            (documento, contexto, cancelacion) =>
            {
                documento.Servers = [];
                return Task.CompletedTask;
            }
        );
    }
);
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var connection = new NpgsqlConnectionStringBuilder
    {
        Host = Required("Tuki:Postgres:Host"),
        Port = int.Parse(Required("Tuki:Postgres:Port")),
        Username = Required("Tuki:Postgres:Usuario"),
        Password = Required("Tuki:Postgres:Password"),
        Database = Required("Tuki:BaseAutenticacion"),
    };

    options.UseNpgsql(connection.ConnectionString).UseSnakeCaseNamingConvention();
});

// Configuración y servicio para emitir los tokens de acceso del login.
// La clave es la misma Jwt__Key que usa el Gateway para validarlos
if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
    throw new InvalidOperationException("Falta Jwt__Key en el .env de la raíz.");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<TokenService>();

// El estado de un tenant solo lo conoce el Administrador; el login lo consulta allí
builder.Services.AddHttpClient<AdministradorClient>(client =>
{
    client.BaseAddress = new Uri(Required("Tuki:Servicios:Administrador"));
});

var app = builder.Build();

// Crea tuki_autenticacion si no existe y aplica sus migraciones
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// La documentación de la API solo se expone en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
