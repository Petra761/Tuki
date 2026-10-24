using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Cuentas.Data;
using Tuki.Cuentas.Dtos;
using Tuki.Cuentas.Models;

namespace Tuki.Cuentas.Controllers;

[ApiController]
[Route("api/cuentas/categories")]
public class CategoriesController(CuentasDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default
    )
    {
        var query = db.Categories.AsNoTracking().AsQueryable();

        // Los archivados se ocultan por defecto: siguen siendo consultables por id
        // para poder restaurarlos con un PUT
        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        var categories = await query.OrderBy(c => c.Name).ToListAsync(ct);
        return Ok(categories.Select(CategoryResponse.From));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return category is null
            ? NotFound(new { error = "La categoría no existe." })
            : Ok(CategoryResponse.From(category));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Kind))
        {
            return BadRequest(new { error = "El tipo de categoría no es válido." });
        }

        if (request.ParentId is { } parentId)
        {
            var parentError = await ValidateParent(parentId, request.Kind, null, ct);
            if (parentError is not null)
            {
                return BadRequest(new { error = parentError });
            }
        }

        var now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            ParentId = request.ParentId,
            Name = request.Name.Trim(),
            Kind = request.Kind,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            CategoryResponse.From(category)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken ct
    )
    {
        if (!Enum.IsDefined(request.Kind))
        {
            return BadRequest(new { error = "El tipo de categoría no es válido." });
        }

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return NotFound(new { error = "La categoría no existe." });
        }

        // El padre solo se valida cuando cambia: así una subcategoría puede
        // restaurarse aunque su madre haya quedado archivada
        if (request.ParentId is { } parentId && parentId != category.ParentId)
        {
            var parentError = await ValidateParent(parentId, request.Kind, id, ct);
            if (parentError is not null)
            {
                return BadRequest(new { error = parentError });
            }
        }

        var hasActiveChildren = await db.Categories.AnyAsync(
            c => c.ParentId == id && !c.IsArchived,
            ct
        );

        // Mantener el tipo fijo mientras haya subcategorías activas evita que
        // queden hijas de un tipo distinto al de su madre
        if (request.Kind != category.Kind && hasActiveChildren)
        {
            return Conflict(
                new
                {
                    error = "No se puede cambiar el tipo de una categoría con subcategorías activas.",
                }
            );
        }

        if (request.IsArchived && !category.IsArchived && hasActiveChildren)
        {
            return Conflict(
                new { error = "No se puede archivar una categoría con subcategorías activas." }
            );
        }

        category.ParentId = request.ParentId;
        category.Name = request.Name.Trim();
        category.Kind = request.Kind;
        category.IsArchived = request.IsArchived;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(CategoryResponse.From(category));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return NotFound(new { error = "La categoría no existe." });
        }

        // La baja es por archivado y es idempotente: las categorías sembradas
        // por la migración jamás se borran físicamente
        if (!category.IsArchived)
        {
            var hasActiveChildren = await db.Categories.AnyAsync(
                c => c.ParentId == id && !c.IsArchived,
                ct
            );

            if (hasActiveChildren)
            {
                return Conflict(
                    new { error = "No se puede archivar una categoría con subcategorías activas." }
                );
            }

            category.IsArchived = true;
            category.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    // Devuelve el motivo por el que el padre no sirve, o null si es válido:
    // debe existir, ser del mismo tipo y ser una categoría principal, porque la
    // estructura admite un solo nivel de profundidad
    private async Task<string?> ValidateParent(
        Guid parentId,
        CategoryKind kind,
        Guid? selfId,
        CancellationToken ct
    )
    {
        if (parentId == selfId)
        {
            return "Una categoría no puede colgar de sí misma.";
        }

        var parent = await db
            .Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == parentId, ct);
        if (parent is null)
        {
            return "La categoría padre no existe.";
        }

        if (parent.IsArchived)
        {
            return "La categoría padre está archivada.";
        }

        if (parent.Kind != kind)
        {
            return "La categoría padre debe ser del mismo tipo.";
        }

        if (parent.ParentId is not null)
        {
            return "Una subcategoría solo puede colgar de una categoría principal.";
        }

        return null;
    }
}
