using System.Net;
using System.Net.Http.Json;
using Tuki.Autenticacion.Dtos;

namespace Tuki.Autenticacion.Services;

// Llamadas al Administrador. Las rutas /internal/* no pasan por el Gateway:
// solo son alcanzables dentro de la máquina.
public class AdministradorClient(HttpClient http)
{
    // Crea un tenant y aprueba sus bases en los servicios de negocio. Si algo
    // falla allí, el resumen igual llega con el estado y el detalle del fallo
    public async Task<ProvisioningSummary> CrearTenantAsync(CancellationToken ct)
    {
        var respuesta = await http.PostAsync("/internal/tenants", null, ct);

        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<ProvisioningSummary>(ct)
            ?? throw new InvalidOperationException(
                "El Administrador devolvió una respuesta vacía."
            );
    }

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
