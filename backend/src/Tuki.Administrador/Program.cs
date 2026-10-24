using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using Tuki.Administrador.Data;
using Tuki.Administrador.OpenApi;
using Tuki.Administrador.Services;

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
        // Quita el segmento /admin de las rutas para que coincidan con las rutas
        // públicas del Gateway (/api/plans, /api/users, etc.)
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
builder.Services.AddHttpClient();
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddDbContext<AdminDbContext>(options =>
{
    var connection = new NpgsqlConnectionStringBuilder
    {
        Host = Required("Tuki:Postgres:Host"),
        Port = int.Parse(Required("Tuki:Postgres:Port")),
        Username = Required("Tuki:Postgres:Usuario"),
        Password = Required("Tuki:Postgres:Password"),
        Database = Required("Tuki:BaseAdministrador"),
    };

    options.UseNpgsql(connection.ConnectionString).UseSnakeCaseNamingConvention();
});

var app = builder.Build();

// Crea tuki_admin si no existe y aplica sus migraciones
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AdminDbContext>().Database.MigrateAsync();
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
