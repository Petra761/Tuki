using System.Net;
using System.Net.Http.Json;

namespace Tuki.Autenticacion.Services;

// Llamadas al Administrador. Las rutas /internal/* no pasan por el Gateway:
// solo son alcanzables dentro de la máquina.
public class AdministradorClient(HttpClient http)
{
    // Estado del tenant ("Active", "Provisioning" o "Failed") tal como lo guarda
    // el Administrador; null si el tenant no existe allí
    public async Task<string?> ObtenerEstadoTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var respuesta = await http.GetAsync($"/internal/tenants/{tenantId}/status", ct);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;

        respuesta.EnsureSuccessStatusCode();

        var estado =
            await respuesta.Content.ReadFromJsonAsync<EstadoTenantResponse>(ct)
            ?? throw new InvalidOperationException(
                "El Administrador devolvió una respuesta vacía."
            );

        return estado.Status;
    }
}

// Respuesta de GET /internal/tenants/{tenantId}/status del Administrador
public record EstadoTenantResponse(Guid TenantId, string Status);
