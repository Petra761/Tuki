namespace Tuki.Administrador.Controllers;

// Lectura de la cabecera X-Tenant-Id: el identificador del tenant viaja allí
// porque la tabla de usuarios ya no está aquí para deducirlo. El Gateway lo
// completa con el de la sesión cuando se exige token; sin sesión, quien llama
// lo envía explícitamente, igual que en los servicios de negocio.
internal static class CabeceraTenant
{
    public static bool TryGet(HttpContext http, out Guid tenantId)
    {
        // Se inicializa primero: con el cortocircuito de && el parámetro out
        // quedaría sin asignar si la primera comprobación falla
        tenantId = Guid.Empty;
        return http.Request.Headers.TryGetValue("X-Tenant-Id", out var values)
            && values.Count == 1
            && Guid.TryParse(values[0], out tenantId)
            && tenantId != Guid.Empty;
    }
}
