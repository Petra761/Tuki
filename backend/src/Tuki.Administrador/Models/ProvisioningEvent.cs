namespace Tuki.Administrador.Models;

public class ProvisioningEvent
{
    public int Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // Servicio al que se pidió crear la base: cuentas, movimientos o presupuestos
    public string Service { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;
    public ProvisionResult Result { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
