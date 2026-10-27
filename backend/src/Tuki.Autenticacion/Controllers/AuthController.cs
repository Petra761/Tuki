using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Autenticacion.Data;
using Tuki.Autenticacion.Dtos;
using Tuki.Autenticacion.Models;
using Tuki.Autenticacion.Services;

namespace Tuki.Autenticacion.Controllers;

[ApiController]
[Route("api/autenticacion/auth")]
public class AuthController(
    AuthDbContext db,
    TokenService tokens,
    AdministradorClient administrador
) : ControllerBase
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    // El nombre del estado activo del tenant, según lo que devuelve el Administrador
    private const string EstadoTenantActivo = "Active";

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Mismo mensaje si el correo no existe o la contraseña falla,
        // para no revelar qué correos están registrados
        if (user is null || user.PasswordHash is null)
            return Unauthorized(new { error = "Correo o contraseña incorrectos." });

        var now = DateTimeOffset.UtcNow;
        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            return StatusCode(
                423,
                new { error = "Cuenta bloqueada temporalmente por intentos fallidos.", lockedUntil }
            );
        }

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = now.Add(LockDuration);
                user.FailedLoginAttempts = 0;
            }
            await db.SaveChangesAsync(ct);
            return Unauthorized(new { error = "Correo o contraseña incorrectos." });
        }

        // Sin bases aprovisionadas, los servicios de negocio no tienen dónde guardar datos.
        // El estado del tenant lo guarda el Administrador: se le consulta antes de emitir
        // el token; un tenant desconocido (null) también se trata como "no activo"
        string? tenantStatus;
        try
        {
            tenantStatus = await administrador.ObtenerEstadoTenantAsync(user.TenantId, ct);
        }
        catch (HttpRequestException)
        {
            // El Administrador no respondió: no se puede saber el estado del tenant
            return StatusCode(
                502,
                new { error = "No se pudo verificar el estado de tu cuenta. Intenta de nuevo." }
            );
        }

        if (tenantStatus != EstadoTenantActivo)
            return Conflict(new { error = "Tu cuenta aún se está preparando. Intenta de nuevo." });

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        // Actualiza el hash si el algoritmo de Identity cambió desde el registro
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, request.Password);

        await db.SaveChangesAsync(ct);

        var (token, expiresAt) = tokens.CreateAccessToken(user);
        var summary = new UserSummary(
            user.Id,
            user.TenantId,
            user.Email,
            user.DisplayName,
            user.EmailVerified
        );

        return Ok(new LoginResponse(token, expiresAt, summary));
    }
}
