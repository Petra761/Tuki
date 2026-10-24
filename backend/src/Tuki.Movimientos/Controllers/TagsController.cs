using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Movimientos.Data;
using Tuki.Movimientos.Dtos;
using Tuki.Movimientos.Models;

namespace Tuki.Movimientos.Controllers;

[ApiController]
[Route("api/movimientos/tags")]
public class TagsController(MovimientosDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default
    )
    {
        var query = db.Tags.AsNoTracking().AsQueryable();

        // Los archivados se ocultan por defecto porque siguen vinculados a
        // transacciones existentes; un PUT los restaura
        if (!includeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        var tags = await query.OrderBy(t => t.Name).ToListAsync(ct);
        return Ok(tags.Select(TagResponse.From));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tag = await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return tag is null
            ? NotFound(new { error = "El marcador no existe." })
            : Ok(TagResponse.From(tag));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTagRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (await db.Tags.AnyAsync(t => t.Name.ToLower() == name.ToLower(), ct))
            return Conflict(new { error = "Ya existe un marcador con ese nombre." });

        var now = DateTimeOffset.UtcNow;
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = tag.Id }, TagResponse.From(tag));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTagRequest request, CancellationToken ct)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null)
        {
            return NotFound(new { error = "El marcador no existe." });
        }

        var name = request.Name.Trim();
        var repeated = await db.Tags.AnyAsync(
            t => t.Id != id && t.Name.ToLower() == name.ToLower(),
            ct
        );

        if (repeated)
        {
            return Conflict(new { error = "Ya existe un marcador con ese nombre." });
        }

        tag.Name = name;
        tag.IsArchived = request.IsArchived;
        tag.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(TagResponse.From(tag));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null)
        {
            return NotFound(new { error = "El marcador no existe." });
        }

        // La baja es por archivado y es idempotente: la clave foránea con
        // restricción impide borrar físicamente un marcador en uso
        if (!tag.IsArchived)
        {
            tag.IsArchived = true;
            tag.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}
