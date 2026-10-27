using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Movimientos.Data;
using Tuki.Movimientos.Dtos;
using Tuki.Movimientos.Models;

namespace Tuki.Movimientos.Controllers;

[ApiController]
[Route("api/movimientos/transactions")]
public class TransactionsController(MovimientosDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? tagId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default
    )
    {
        // Los valores fuera de rango se llevan al permitido en vez de fallar:
        // un listado nunca debe rechazarse por el tamaño de página pedido
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 50;
        }

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var query = db.Transactions.AsNoTracking().AsQueryable();

        if (from is not null)
        {
            query = query.Where(t => t.OccurredOn >= from);
        }

        if (to is not null)
        {
            query = query.Where(t => t.OccurredOn <= to);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t =>
                t.Description != null && t.Description.ToLower().Contains(term)
            );
        }

        if (accountId is not null)
        {
            query = query.Where(t => t.Lines.Any(l => l.AccountId == accountId));
        }

        if (categoryId is not null)
        {
            query = query.Where(t => t.Lines.Any(l => l.CategoryId == categoryId));
        }

        if (tagId is not null)
        {
            query = query.Where(t => t.TransactionTags.Any(tt => tt.TagId == tagId));
        }

        var total = await query.CountAsync(ct);

        var transactions = await query
            .Include(t => t.Lines)
            .Include(t => t.TransactionTags)
            .OrderByDescending(t => t.OccurredOn)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = transactions.Select(TransactionResponse.From).ToList();
        return Ok(new TransactionListResponse(items, page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var transaction = await db
            .Transactions.AsNoTracking()
            .Include(t => t.Lines)
            .Include(t => t.TransactionTags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return transaction is null
            ? NotFound(new { error = "La transacción no existe." })
            : Ok(TransactionResponse.From(transaction));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionRequest request, CancellationToken ct)
    {
        var (error, type) = await Validate(request, ct);
        if (error is not null)
        {
            return BadRequest(new { error });
        }

        var lines = request.Lines ?? [];
        if (type == TransactionType.Opening)
        {
            var conflict = await OpeningConflict(lines, Guid.Empty, ct);
            if (conflict is not null)
            {
                return Conflict(new { error = conflict });
            }
        }

        var tagIds = request.TagIds ?? [];
        var now = DateTimeOffset.UtcNow;
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Type = type,
            OccurredOn = request.OccurredOn,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Lines = lines.Select(l => NewLine(Guid.NewGuid(), l, now)).ToList(),
            TransactionTags = tagIds
                .Distinct()
                .Select(tagId => new TransactionTag { TagId = tagId })
                .ToList(),
        };

        db.Transactions.Add(transaction);

        // Un único SaveChanges: la transacción con sus líneas y marcadores se
        // guarda de forma atómica o no se guarda nada
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = transaction.Id },
            TransactionResponse.From(transaction)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTransactionRequest request,
        CancellationToken ct
    )
    {
        var transaction = await db
            .Transactions.Include(t => t.Lines)
            .Include(t => t.TransactionTags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (transaction is null)
        {
            return NotFound(new { error = "La transacción no existe." });
        }

        var (error, type) = await Validate(request, ct);
        if (error is not null)
        {
            return BadRequest(new { error });
        }

        var lines = request.Lines ?? [];
        if (type == TransactionType.Opening)
        {
            var conflict = await OpeningConflict(lines, id, ct);
            if (conflict is not null)
            {
                return Conflict(new { error = conflict });
            }
        }

        var tagIds = request.TagIds ?? [];
        var now = DateTimeOffset.UtcNow;

        await using (var dbTransaction = await db.Database.BeginTransactionAsync(ct))
        {
            // Primero se retiran las líneas y vínculos viejos y se guarda: si el
            // PUT reutiliza un marcador que ya estaba asignado, insertarlo antes
            // de borrar violaría la clave compuesta (transacción + marcador)
            db.TransactionLines.RemoveRange(transaction.Lines);
            db.TransactionTags.RemoveRange(transaction.TransactionTags);
            await db.SaveChangesAsync(ct);

            transaction.Type = type;
            transaction.OccurredOn = request.OccurredOn;
            transaction.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
            transaction.UpdatedAt = now;
            transaction.Lines = lines.Select(l => NewLine(Guid.NewGuid(), l, now)).ToList();
            transaction.TransactionTags = tagIds
                .Distinct()
                .Select(tagId => new TransactionTag { TransactionId = id, TagId = tagId })
                .ToList();

            await db.SaveChangesAsync(ct);
            await dbTransaction.CommitAsync(ct);
        }

        return Ok(TransactionResponse.From(transaction));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (transaction is null)
        {
            return NotFound(new { error = "La transacción no existe." });
        }

        // Borrado físico: las líneas y los vínculos de marcadores caen en cascada
        // (onDelete: Cascade en la migración)
        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // Valida el cuerpo y deduce el tipo de la transacción a partir de sus líneas,
    // según las reglas del dominio documentadas en movimientos.dbml
    private async Task<(string? Error, TransactionType Type)> Validate(
        TransactionBody request,
        CancellationToken ct
    )
    {
        if (request.OccurredOn == default)
        {
            return ("La fecha es obligatoria.", default);
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return ("La descripción es obligatoria.", default);
        }

        var lines = request.Lines ?? [];
        if (lines.Count == 0)
        {
            return ("La transacción necesita al menos una línea.", default);
        }

        var withCategory = lines.Where(l => l.CategoryId is not null).ToList();

        foreach (var line in lines)
        {
            if (line.AccountId == Guid.Empty)
            {
                return ("Cada línea necesita una cuenta.", default);
            }

            if (line.AmountCents == 0)
            {
                return ("El importe de una línea no puede ser 0.", default);
            }
        }

        TransactionType type;

        // El tipo se deduce de la forma de las líneas, sin preguntar a Cuentas:
        // la regla de que la categoría sea del mismo tipo que la transacción no
        // aplica aquí porque no hay llamadas HTTP entre servicios
        if (withCategory.Count == lines.Count)
        {
            // Gasto e ingreso: todas las líneas llevan categoría y comparten signo
            if (lines.All(l => l.AmountCents < 0))
            {
                type = TransactionType.Expense;
            }
            else if (lines.All(l => l.AmountCents > 0))
            {
                type = TransactionType.Income;
            }
            else
            {
                return (
                    "Las líneas de un gasto son negativas y las de un ingreso positivas.",
                    default
                );
            }
        }
        else if (withCategory.Count > 0)
        {
            return ("O todas las líneas llevan categoría o ninguna.", default);
        }
        else if (lines.Count == 1)
        {
            // Apertura: una sola línea sin categoría; el monto puede ser positivo
            // o negativo porque una persona puede empezar debiendo
            type = TransactionType.Opening;
        }
        else if (lines.Sum(l => l.AmountCents) == 0)
        {
            // Traslado: dos o más líneas sin categoría que se compensan a cero
            type = TransactionType.Transfer;
        }
        else
        {
            return ("Un movimiento sin categoría debe sumar 0 para ser un traslado.", default);
        }

        var tagIds = (request.TagIds ?? []).ToList();
        if (tagIds.Distinct().Count() != tagIds.Count)
        {
            return ("Hay marcadores repetidos.", default);
        }

        if (tagIds.Count > 0)
        {
            var valid = await db
                .Tags.Where(t => tagIds.Contains(t.Id) && !t.IsArchived)
                .Select(t => t.Id)
                .ToListAsync(ct);

            if (valid.Count != tagIds.Distinct().Count())
            {
                return ("Un marcador no existe o está archivado.", default);
            }
        }

        return (null, type);
    }

    // Cada cuenta admite como máximo una apertura
    private async Task<string?> OpeningConflict(
        List<TransactionLineInput> lines,
        Guid currentId,
        CancellationToken ct
    )
    {
        var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();

        var repeated = await db
            .Transactions.Where(t =>
                t.Id != currentId
                && t.Type == TransactionType.Opening
                && t.Lines.Any(l => accountIds.Contains(l.AccountId))
            )
            .AnyAsync(ct);

        // Cada cuenta admite como máximo una apertura
        return repeated ? "Ya existe una apertura para esa cuenta." : null;
    }

    private static TransactionLine NewLine(
        Guid id,
        TransactionLineInput input,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = id,
            AccountId = input.AccountId,
            CategoryId = input.CategoryId,
            AmountCents = input.AmountCents,
            CreatedAt = now,
            UpdatedAt = now,
        };
}
