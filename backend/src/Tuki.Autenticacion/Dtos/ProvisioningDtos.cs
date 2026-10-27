namespace Tuki.Autenticacion.Dtos;

// Contrato de las respuestas del Administrador en /internal/tenants: la forma
// es idéntica a la de sus propios tipos, porque viajan por JSON entre servicios

// GET /internal/tenants/{tenantId}/status
public record EstadoTenantResponse(Guid TenantId, string Status);

// POST /internal/tenants
public record ProvisioningSummary(Guid TenantId, string Status, List<ProvisionDetail> Services);

// Lo que preparó cada servicio de negocio al crear el tenant
public record ProvisionDetail(string Service, string Database, string Result, string? Error);
