namespace Tuki.Administrador.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public TenantStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
    public List<ProvisioningEvent> ProvisioningEvents { get; set; } = [];
}
