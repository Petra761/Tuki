using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Cuentas.Data;
using Tuki.Cuentas.Dtos;
using Tuki.Cuentas.Models;

namespace Tuki.Cuentas.Controllers;

[ApiController]
[Route("api/cuentas/accounts")]
public class AccountsController(CuentasDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default
    )
    {
        var query = db.Accounts.AsNoTracking().Include(a => a.AccountType).AsQueryable();

        // Los archivados se ocultan por defecto porque conservan su historial de
        // movimientos y un PUT los restaura
        if (!includeArchived)
        {
            query = query.Where(a => !a.IsArchived);
        }

        var accounts = await query.OrderBy(a => a.Name).ToListAsync(ct);

        return Ok(accounts.Select(a => AccountResponse.From(a, a.AccountType.Name)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var account = await db
            .Accounts.AsNoTracking()
            .Include(a => a.AccountType)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return account is null
            ? NotFound(new { error = "La cuenta no existe." })
            : Ok(AccountResponse.From(account, account.AccountType.Name));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var type = await db
            .AccountTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.AccountTypeId && !t.IsArchived, ct);

        if (type is null)
            return BadRequest(new { error = "El tipo de cuenta no existe o está archivado." });

        var now = DateTimeOffset.UtcNow;
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountTypeId = type.Id,
            Name = request.Name.Trim(),
            CurrencyCode = "BOB",
            IsDefault = false,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = account.Id },
            AccountResponse.From(account, type.Name)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateAccountRequest request,
        CancellationToken ct
    )
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (account is null)
        {
            return NotFound(new { error = "La cuenta no existe." });
        }

        // Al crear o actualizar, la cuenta debe apuntar a un tipo existente y
        // activo: un tipo archivado no admite cuentas nuevas ni cambios
        var type = await db.AccountTypes.FirstOrDefaultAsync(
            t => t.Id == request.AccountTypeId && !t.IsArchived,
            ct
        );

        if (type is null)
        {
            return BadRequest(new { error = "El tipo de cuenta no existe o está archivado." });
        }

        // El índice único filtrado solo admite una fila con is_default = true.
        // Primero se libera la marca de la anterior (en la misma transacción) y
        // recién entonces se reclama la nueva: si ambas cosas viajaran en un
        // solo SaveChanges, el orden de los UPDATE no está garantizado y el
        // índice tiraría una violación transitoria
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        if (request.IsDefault && !account.IsDefault)
        {
            var previousDefaults = await db
                .Accounts.Where(a => a.IsDefault && a.Id != id)
                .ToListAsync(ct);

            if (previousDefaults.Count > 0)
            {
                foreach (var previous in previousDefaults)
                {
                    previous.IsDefault = false;
                }

                await db.SaveChangesAsync(ct);
            }
        }

        account.AccountTypeId = type.Id;
        account.Name = request.Name.Trim();
        account.IsDefault = request.IsDefault;
        account.IsArchived = request.IsArchived;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(AccountResponse.From(account, type.Name));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (account is null)
        {
            return NotFound(new { error = "La cuenta no existe." });
        }

        // La baja es por archivado, no por borrado: los movimientos siguen
        // refiriéndose a la cuenta, por eso la cuenta Efectivo sembrada por la
        // migración tampoco puede desaparecer
        if (!account.IsArchived)
        {
            account.IsArchived = true;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}
