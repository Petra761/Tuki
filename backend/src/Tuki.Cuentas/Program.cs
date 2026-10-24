using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using Tuki.Cuentas.Data;
using Tuki.Cuentas.Middleware;
using Tuki.Cuentas.OpenApi;

// Carga el archivo .env buscándolo hacia arriba desde la carpeta actual. Debe ir ANTES de
// crear el builder: así sus valores ya están en las variables de entorno cuando
// ASP.NET Core arma la configuración.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Nombre corto de este servicio; forma parte del nombre de sus bases (ej. tuki_cuentas_12).
const string servicio = "cuentas";

// Los enums se envían como texto ("Expense", "Income") para que la API sea
// legible, igual que en Presupuestos
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddOpenApi(
    "publico",
    options =>
    {
        // Quita el segmento /cuentas de las rutas para que coincidan con las rutas
        // públicas del Gateway (/api/accounts, etc.)
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
builder.Services.AddSingleton(new ConexionesTenant(builder.Configuration, servicio));

builder.Services.AddSingleton<TenantDatabaseProvisioner>();

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<CuentasDbContext>(
    (services, options) =>
    {
        var tenant = services.GetRequiredService<TenantContext>();
        if (tenant.TenantId == Guid.Empty)
            throw new InvalidOperationException("No hay un tenant seleccionado.");

        var conexiones = services.GetRequiredService<ConexionesTenant>();
        options
            .UseNpgsql(conexiones.CadenaConexion(tenant.TenantId))
            .UseSnakeCaseNamingConvention();
    }
);

var app = builder.Build();

// Prueba temporal: confirma al arrancar que las credenciales del .env sirven.
// Se elimina cuando exista el DbContext.
await using (
    var conexion = new NpgsqlConnection(
        app.Services.GetRequiredService<ConexionesTenant>().CadenaConexionServidor()
    )
)
{
    await conexion.OpenAsync();
    app.Logger.LogInformation(
        "Conexión a PostgreSQL correcta (versión {Version}).",
        conexion.PostgreSqlVersion
    );
}

app.UseHttpsRedirection();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();

// La documentación de la API solo se expone en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
