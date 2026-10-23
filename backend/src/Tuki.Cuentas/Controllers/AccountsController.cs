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
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var accounts = await db
            .Accounts.AsNoTracking()
            .Include(a => a.AccountType)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

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
}
