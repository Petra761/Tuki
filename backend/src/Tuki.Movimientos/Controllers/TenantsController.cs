using Microsoft.AspNetCore.Mvc;
using Tuki.Movimientos.Data;

namespace Tuki.Movimientos.Controllers;

[ApiController]
[Route("internal/tenants")]
public class TenantsController(
    TenantDatabaseProvisioner provisioner,
    ILogger<TenantsController> logger) : ControllerBase
{
    [HttpPost("{tenantId:guid}/database")]
    public async Task<IActionResult> PrepareDatabase(Guid tenantId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
            return BadRequest("El tenant no es válido.");

        try
        {
            var outcome = await provisioner.PrepareAsync(tenantId, ct);
            return outcome.Result == "Created" ? StatusCode(201, outcome) : Ok(outcome);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo preparar la base del tenant {TenantId}.", tenantId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
