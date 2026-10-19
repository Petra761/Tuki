using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Tuki.Movimientos.Data;
using Tuki.Movimientos.Middleware;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

const string servicio = "movimientos";

builder.Services.AddControllers();
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

app.Run();
