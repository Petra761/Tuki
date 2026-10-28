using Scalar.AspNetCore;
using Microsoft.OpenApi;
using Tuki.Reportes.OpenApi;
using Tuki.Reportes.Services;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient("Cuentas", client =>
{
    client.BaseAddress = ServiceAddress(builder.Configuration, "Tuki:Servicios:Cuentas");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("Movimientos", client =>
{
    client.BaseAddress = ServiceAddress(builder.Configuration, "Tuki:Servicios:Movimientos");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("Presupuestos", client =>
{
    client.BaseAddress = ServiceAddress(builder.Configuration, "Tuki:Servicios:Presupuestos");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ReportsService>();
builder.Services.AddOpenApi();
builder.Services.AddOpenApi(
    "publico",
    options =>
    {
        options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();

        // Quita el segmento /reportes de las rutas para que coincidan con las rutas
        // públicas del Gateway (/api/reports, etc.)
        options.AddDocumentTransformer<SegmentoPublicoTransformer>();

        // Sin hosts absolutos: Scalar calcula la URL base según desde dónde se abra,
        // así las pruebas por el devtunnel apuntan al túnel y no a localhost
        options.AddDocumentTransformer(
            (documento, contexto, cancelacion) =>
            {
                documento.Servers = [];
                return Task.CompletedTask;
            }
        );

        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = "X-Tenant-Id",
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Tenant del token de sesión autenticado.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
                }
            );
            return Task.CompletedTask;
        });
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static Uri ServiceAddress(IConfiguration configuration, string key)
{
    var address = configuration[key];
    if (
        !Uri.TryCreate(address, UriKind.Absolute, out var uri)
        || uri.Scheme != Uri.UriSchemeHttps
    )
    {
        throw new InvalidOperationException(
            $"La configuración {key} debe contener una URL HTTPS absoluta."
        );
    }

    return new Uri(uri.AbsoluteUri.TrimEnd('/') + '/', UriKind.Absolute);
}
