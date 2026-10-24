using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Movimientos.Data;
using Tuki.Movimientos.Dtos;
using Tuki.Movimientos.Models;

namespace Tuki.Movimientos.Controllers;

[ApiController]
[Route("api/movimientos/shortcuts")]
public class ShortcutsController(MovimientosDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default
    )
    {
        var query = db.Shortcuts.AsNoTracking().AsQueryable();

        // Los archivados se ocultan por defecto: un PUT los restaura
        if (!includeArchived)
        {
            query = query.Where(s => !s.IsArchived);
        }

        var shortcuts = await query.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(ct);
        return Ok(shortcuts.Select(ShortcutResponse.From));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var shortcut = await db.Shortcuts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return shortcut is null
            ? NotFound(new { error = "El atajo no existe." })
            : Ok(ShortcutResponse.From(shortcut));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateShortcutRequest request, CancellationToken ct)
    {
        var idsError = ValidateIds(request.CategoryId, request.AccountId);
        if (idsError is not null)
        {
            return BadRequest(new { error = idsError });
        }

        var now = DateTimeOffset.UtcNow;
        var shortcut = new Shortcut
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            AmountCents = request.AmountCents,
            CategoryId = request.CategoryId,
            AccountId = request.AccountId,
            SortOrder = request.SortOrder,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Shortcuts.Add(shortcut);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = shortcut.Id },
            ShortcutResponse.From(shortcut)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateShortcutRequest request,
        CancellationToken ct
    )
    {
        var shortcut = await db.Shortcuts.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shortcut is null)
        {
            return NotFound(new { error = "El atajo no existe." });
        }

        var idsError = ValidateIds(request.CategoryId, request.AccountId);
        if (idsError is not null)
        {
            return BadRequest(new { error = idsError });
        }

        shortcut.Name = request.Name.Trim();
        shortcut.AmountCents = request.AmountCents;
        shortcut.CategoryId = request.CategoryId;
        shortcut.AccountId = request.AccountId;
        shortcut.SortOrder = request.SortOrder;
        shortcut.IsArchived = request.IsArchived;
        shortcut.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ShortcutResponse.From(shortcut));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var shortcut = await db.Shortcuts.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shortcut is null)
        {
            return NotFound(new { error = "El atajo no existe." });
        }

        // La baja es por archivado y es idempotente: los atajos guardados no
        // deben desaparecer si alguien los reordena en la pantalla principal
        if (!shortcut.IsArchived)
        {
            shortcut.IsArchived = true;
            shortcut.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    // AccountId y CategoryId son solo identificadores del servicio Cuentas: la
    // presencia se comprueba aquí, pero la existencia no se consulta
    private static string? ValidateIds(Guid categoryId, Guid accountId) =>
        categoryId == Guid.Empty || accountId == Guid.Empty
            ? "La cuenta y la categoría del atajo son obligatorias."
            : null;
}
