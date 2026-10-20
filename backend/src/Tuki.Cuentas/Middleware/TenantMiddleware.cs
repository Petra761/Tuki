namespace Tuki.Cuentas.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, TenantContext tenant)
    {
        // Las rutas internas y la documentación de la API no pertenecen a ningún tenant
        if (
            http.Request.Path.StartsWithSegments("/internal")
            || http.Request.Path.StartsWithSegments("/scalar")
            || http.Request.Path.StartsWithSegments("/openapi")
        )
        {
            await next(http);
            return;
        }

        if (
            !http.Request.Headers.TryGetValue("X-Tenant-Id", out var values)
            || values.Count != 1
            || !Guid.TryParse(values[0], out var tenantId)
            || tenantId == Guid.Empty
        )
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await http.Response.WriteAsync("Debes enviar un X-Tenant-Id valido.");
            return;
        }

        tenant.TenantId = tenantId;
        await next(http);
    }
}
