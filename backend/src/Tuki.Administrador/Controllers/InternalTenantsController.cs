using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Data;
using Tuki.Administrador.Models;
using Tuki.Administrador.Services;

namespace Tuki.Administrador.Controllers;

// Las rutas /internal/* son solo para los otros servicios de la misma máquina:
// el Gateway no las enruta y no aparecen en el documento OpenAPI público
[ApiController]
[Route("internal/tenants")]
public class InternalTenantsController(AdminDbContext db, TenantProvisioningService provisioning)
    : ControllerBase
{
    // Autenticación lo crea desde el registro de usuarios y lo consulta en cada
    // login para mantener el 409 de "tenant no activo"
    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Status = TenantStatus.Provisioning,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Tenants.Add(tenant);

        // Prepara las bases del tenant en los servicios de negocio; dentro del
        // servicio se guardan el tenant y sus eventos de aprovisionamiento
        var summary = await provisioning.ProvisionAsync(tenant, ct);

        // Siempre 200: el resultado (Active o Failed) viaja en el cuerpo,
        // quien llama decide qué hacer con él
        return Ok(summary);
    }

    // El estado del tenant vive aquí, no en la base de credenciales: lo consulta
    // Autenticación en cada login antes de emitir un token
    [HttpGet("{tenantId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
            return NotFound(new { error = "El tenant no existe." });

        return Ok(new { TenantId = tenant.Id, Status = tenant.Status.ToString() });
    }
}
