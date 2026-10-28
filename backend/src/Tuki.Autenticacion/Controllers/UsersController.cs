using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Autenticacion.Data;
using Tuki.Autenticacion.Dtos;
using Tuki.Autenticacion.Models;
using Tuki.Autenticacion.Services;

namespace Tuki.Autenticacion.Controllers;

[ApiController]
[Route("api/autenticacion/users")]
public class UsersController(
    AuthDbContext db,
    AdministradorClient administrador,
    ILogger<UsersController> logger
) : ControllerBase
{
    // El nombre del estado activo del tenant, según lo que devuelve el Administrador
    private const string EstadoTenantActivo = "Active";

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { error = "Ya existe un usuario con ese correo." });

        // El tenant y sus bases los crea y aprueba el Administrador
        ProvisioningSummary provisioning;
        try
        {
            provisioning = await administrador.CrearTenantAsync(ct);
        }
        catch (HttpRequestException)
        {
            // Sin respuesta del Administrador no hay tenant al que asociar al usuario
            return StatusCode(
                502,
                new { error = "No se pudo preparar tu cuenta. Intenta de nuevo." }
            );
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = provisioning.TenantId,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var password = request.Password ?? GenerateTemporaryPassword();
        var passwordTemporal = request.Password is null ? password : null;
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        // El usuario se guarda aunque el aprovisionamiento haya fallado: es lo que
        // permite después recuperar la cuenta cuando el tenant quede Active
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Dos registros simultáneos con el mismo correo: el índice único lo
            // impide. El tenant creado queda huérfano, que es aceptable
            logger.LogWarning(
                "No se pudo guardar el usuario {Email}: correo duplicado por una carrera.",
                email
            );
            return Conflict(new { error = "Ya existe un usuario con ese correo." });
        }

        var body = new
        {
            userId = user.Id,
            provisioning.TenantId,
            provisioning.Status,
            provisioning.Services,
            passwordTemporal,
        };

        return provisioning.Status == EstadoTenantActivo
            ? StatusCode(201, body)
            : StatusCode(502, body);
    }

    private static string GenerateTemporaryPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
