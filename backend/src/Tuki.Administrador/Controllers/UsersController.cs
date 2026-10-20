using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Data;
using Tuki.Administrador.Dtos;
using Tuki.Administrador.Models;
using Tuki.Administrador.Services;

namespace Tuki.Administrador.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(AdminDbContext db, TenantProvisioningService provisioning)
    : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { error = "Ya existe un usuario con ese correo." });

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Status = TenantStatus.Provisioning,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var summary = await provisioning.ProvisionAsync(tenant, ct);
        var body = new
        {
            userId = user.Id,
            summary.TenantId,
            summary.Status,
            summary.Services,
        };

        return tenant.Status == TenantStatus.Active ? StatusCode(201, body) : StatusCode(502, body);
    }
}
