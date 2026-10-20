namespace Tuki.Administrador.Dtos;

// Respuesta que devuelve cada servicio de negocio al preparar su base
public record ServiceProvisionResponse(string Database, string Result, int MigrationsApplied);

public record ProvisionDetail(string Service, string Database, string Result, string? Error);

public record ProvisioningSummary(Guid TenantId, string Status, List<ProvisionDetail> Services);
