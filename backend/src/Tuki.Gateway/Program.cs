using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// ---- Servicios: todo lo de builder.Services va antes de Build() ----

builder
    .Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta Jwt__Key en el .env de la raíz.");

// Los valores de emisor y audiencia deben coincidir con los que usa el servicio de Autenticación al emitir el token
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"] ?? "Tuki",
            ValidAudience = jwt["Audience"] ?? "Tuki",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

// La política de respaldo cubre todas las rutas sin política propia; las públicas se marcan como "anonymous"
var exigirSesion = builder.Configuration.GetValue<bool>("Tuki:Auth:Exigir");
builder.Services.AddAuthorization(options =>
{
    if (exigirSesion)
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

// El CORS se resuelve en el Gateway porque es el único punto de entrada: los
// microservicios no necesitan repetir esta lógica. Es obligatorio porque todos
// exigen el header X-Tenant-Id, que el navegador trata como "no simple" y por
// eso dispara un preflight OPTIONS antes de cualquier petición real.
var origenesPermitidos =
    builder.Configuration.GetSection("Tuki:Cors:Origenes").Get<string[]>() ?? [];

bool OrigenPermitido(string origin)
{
    if (origenesPermitidos.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.EndsWith(".on.expo.app", StringComparison.OrdinalIgnoreCase);
}

builder.Services.AddCors(options =>
    options.AddPolicy(
        "tuki",
        politica =>
            politica
                .SetIsOriginAllowed(OrigenPermitido)
                // X-Tenant-Id es la cabecera con la que cada cliente identifica su base
                .WithHeaders("Content-Type", "X-Tenant-Id", "Authorization")
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                // Necesario para que el probador vea este header al validar el aislamiento
                .WithExposedHeaders("X-Tuki-Gateway")
    )
);

var app = builder.Build();

// ---- Pipeline: todo lo de app. va después de Build() ----

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
        {
            options
                .AddDocument("autenticacion", "Autenticación", "/openapi/autenticacion.json")
                .AddDocument("administrador", "Administrador", "/openapi/administrador.json")
                .AddDocument("cuentas", "Cuentas", "/openapi/cuentas.json")
                .AddDocument("movimientos", "Movimientos", "/openapi/movimientos.json")
                .AddDocument("presupuestos", "Presupuestos", "/openapi/presupuestos.json")
                // Toma la URL base de la petición actual, así funciona igual en localhost y en el devtunnel
                .WithDynamicBaseServerUrl();
        })
        .AllowAnonymous();
}

app.MapGet("/", () => "Hello World!").AllowAnonymous();

// Antes del proxy para que también cubra las respuestas enrutadas
app.UseCors("tuki");

app.UseAuthentication();
app.UseAuthorization();

// Con sesión iniciada, el tenant sale del token: un usuario no puede consultar datos de otro tenant cambiando la cabecera
app.Use(
    async (context, next) =>
    {
        var claim = context.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(claim, out var tenantDelToken))
        {
            var cabecera = context.Request.Headers["X-Tenant-Id"].ToString();
            if (
                cabecera.Length > 0
                && !(
                    Guid.TryParse(cabecera, out var tenantEnviado)
                    && tenantEnviado == tenantDelToken
                )
            )
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                    new { error = "El tenant de la cabecera no coincide con el de tu sesión." }
                );
                return;
            }
            context.Request.Headers["X-Tenant-Id"] = tenantDelToken.ToString();
        }
        await next();
    }
);

app.MapReverseProxy();

app.Run();
