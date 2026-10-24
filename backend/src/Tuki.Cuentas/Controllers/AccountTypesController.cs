using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Cuentas.Data;
using Tuki.Cuentas.Dtos;
using Tuki.Cuentas.Models;

namespace Tuki.Cuentas.Controllers;

[ApiController]
[Route("api/cuentas/account-types")]
public class AccountTypesController(CuentasDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default
    )
    {
        var query = db.AccountTypes.AsNoTracking().AsQueryable();

        // Los archivados se ocultan por defecto: siguen siendo consultables por id
        // para poder restaurarlos con un PUT
        if (!includeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        var types = await query.OrderBy(t => t.Name).ToListAsync(ct);
        return Ok(types.Select(AccountTypeResponse.From));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var accountType = await db
            .AccountTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return accountType is null
            ? NotFound(new { error = "El tipo de cuenta no existe." })
            : Ok(AccountTypeResponse.From(accountType));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountTypeRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.AccountTypes.AnyAsync(t => t.Name == name, ct))
        {
            return Conflict(new { error = "Ya existe un tipo de cuenta con ese nombre." });
        }

        var now = DateTimeOffset.UtcNow;
        var accountType = new AccountType
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsPerson = request.IsPerson,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.AccountTypes.Add(accountType);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = accountType.Id },
            AccountTypeResponse.From(accountType)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateAccountTypeRequest request,
        CancellationToken ct
    )
    {
        var accountType = await db.AccountTypes.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (accountType is null)
        {
            return NotFound(new { error = "El tipo de cuenta no existe." });
        }

        var name = request.Name.Trim();
        var repeated = await db.AccountTypes.AnyAsync(t => t.Id != id && t.Name == name, ct);

        if (repeated)
        {
            return Conflict(new { error = "Ya existe un tipo de cuenta con ese nombre." });
        }

        accountType.Name = name;
        accountType.IsPerson = request.IsPerson;
        accountType.IsArchived = request.IsArchived;
        accountType.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(AccountTypeResponse.From(accountType));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var accountType = await db.AccountTypes.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (accountType is null)
        {
            return NotFound(new { error = "El tipo de cuenta no existe." });
        }

        // La baja es por archivado y es idempotente: además, los tipos sembrados
        // por la migración jamás se borran físicamente
        if (!accountType.IsArchived)
        {
            accountType.IsArchived = true;
            accountType.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}
