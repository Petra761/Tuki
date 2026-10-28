using Microsoft.AspNetCore.Mvc;

namespace Tuki.Reportes.Controllers;

[ApiController]
[Route("api/reportes/reports/ping")]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { servicio = "Reportes", estado = "Activo" });
    }
}
