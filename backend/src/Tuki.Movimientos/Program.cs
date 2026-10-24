using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Tuki.Movimientos.Data;
using Tuki.Movimientos.Middleware;
using Tuki.Movimientos.OpenApi;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

const string servicio = "movimientos";

// Los enums del dominio viajan como texto (type: "Expense") para que el
// contrato JSON no dependa de los valores numéricos del backend
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
    );
builder.Services.AddOpenApi();
builder.Services.AddOpenApi(
    "publico",
    options => options.AddDocumentTransformer<SegmentoPublicoTransformer>()
);
builder.Services.AddSingleton(new ConexionesTenant(builder.Configuration, servicio));
builder.Services.AddSingleton<TenantDatabaseProvisioner>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<MovimientosDbContext>(
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
