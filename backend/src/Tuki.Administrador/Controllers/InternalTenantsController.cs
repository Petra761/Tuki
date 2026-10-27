using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Data;

namespace Tuki.Administrador.Controllers;

// Las rutas /internal/* son solo para los otros servicios de la misma máquina:
// el Gateway no las enruta y no aparecen en el documento OpenAPI público
[ApiController]
[Route("internal/tenants")]
public class InternalTenantsController(AdminDbContext db) : ControllerBase
{
    // Autenticación lo consulta en cada login para mantener el 409 de "tenant no
    // activo": el estado del tenant vive aquí, no en la base de credenciales
    [HttpGet("{tenantId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
            return NotFound(new { error = "El tenant no existe." });

        return Ok(new { TenantId = tenant.Id, Status = tenant.Status.ToString() });
    }
}
