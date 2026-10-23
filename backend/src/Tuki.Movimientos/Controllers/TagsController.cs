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
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tags = await db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
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
}
