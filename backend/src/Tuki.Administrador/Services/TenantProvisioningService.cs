using Tuki.Administrador.Data;
using Tuki.Administrador.Dtos;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Services;

public class TenantProvisioningService(
    AdminDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TenantProvisioningService> logger
)
{
    public async Task<ProvisioningSummary> ProvisionAsync(Tenant tenant, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var details = new List<ProvisionDetail>();

        // Todos los servicios de la configuración, menos el propio Administrador
        var services = configuration
            .GetSection("Tuki:Servicios")
            .GetChildren()
            .Where(s => !s.Key.Equals("Administrador", StringComparison.OrdinalIgnoreCase));

        foreach (var service in services)
        {
            var name = service.Key.ToLowerInvariant();

            // El nombre de la base queda vacío si el servicio nunca respondió: no se puede conocer
            var evt = new ProvisioningEvent
            {
                TenantId = tenant.Id,
                Service = name,
                DatabaseName = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            try
            {
                var response = await client.PostAsync(
                    $"{service.Value}/internal/tenants/{tenant.Id}/database",
                    null,
                    ct
                );

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidOperationException(
                        $"El servicio respondió {(int)response.StatusCode}: {body}"
                    );
                }

                var outcome =
                    await response.Content.ReadFromJsonAsync<ServiceProvisionResponse>(ct)
                    ?? throw new InvalidOperationException(
                        "El servicio devolvió una respuesta vacía."
                    );

                evt.DatabaseName = outcome.Database;
                evt.Result = Enum.Parse<ProvisionResult>(outcome.Result);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "No se pudo preparar la base de {Service} para el tenant {TenantId}.",
                    name,
                    tenant.Id
                );
                evt.Result = ProvisionResult.Failed;
                evt.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            }

            db.ProvisioningEvents.Add(evt);
            details.Add(
                new ProvisionDetail(name, evt.DatabaseName, evt.Result.ToString(), evt.Error)
            );
        }

        // Sin servicios configurados tampoco se considera éxito
        var allSucceeded =
            details.Count > 0 && details.All(d => d.Result != nameof(ProvisionResult.Failed));
        tenant.Status = allSucceeded ? TenantStatus.Active : TenantStatus.Failed;

        await db.SaveChangesAsync(ct);
        return new ProvisioningSummary(tenant.Id, tenant.Status.ToString(), details);
    }
}
