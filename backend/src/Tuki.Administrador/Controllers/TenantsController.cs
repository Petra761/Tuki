using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Data;
using Tuki.Administrador.Models;
using Tuki.Administrador.Services;

namespace Tuki.Administrador.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController(AdminDbContext db, TenantProvisioningService provisioning)
    : ControllerBase
{
    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db
            .Tenants.Include(t => t.ProvisioningEvents)
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
            return NotFound(new { error = "El tenant no existe." });

        return Ok(
            new
            {
                tenant.Id,
                Status = tenant.Status.ToString(),
                Events = tenant
                    .ProvisioningEvents.OrderBy(e => e.Id)
                    .Select(e => new
                    {
                        e.Service,
                        e.DatabaseName,
                        Result = e.Result.ToString(),
                        e.Error,
                        e.CreatedAt,
                    }),
            }
        );
    }

    // Repite la preparación de un tenant existente: los servicios no duplican lo que ya existe
    [HttpPost("{tenantId:guid}/provision")]
    public async Task<IActionResult> Provision(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null)
            return NotFound(new { error = "El tenant no existe." });

        var summary = await provisioning.ProvisionAsync(tenant, ct);
        return tenant.Status == TenantStatus.Active ? Ok(summary) : StatusCode(502, summary);
    }
}
