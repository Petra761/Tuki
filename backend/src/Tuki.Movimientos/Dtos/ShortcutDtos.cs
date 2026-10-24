using System.ComponentModel.DataAnnotations;
using Tuki.Movimientos.Models;

namespace Tuki.Movimientos.Dtos;

public class CreateShortcutRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100000000000)]
    public long AmountCents { get; set; }

    // Identificadores del servicio Cuentas: se guardan tal cual, sin validarlos
    // aquí, porque este servicio no consulta a los demás
    public Guid CategoryId { get; set; }

    public Guid AccountId { get; set; }

    public int SortOrder { get; set; }
}

// El PUT reemplaza el atajo; IsArchived permite restaurarlo
public class UpdateShortcutRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100000000000)]
    public long AmountCents { get; set; }

    public Guid CategoryId { get; set; }

    public Guid AccountId { get; set; }

    public int SortOrder { get; set; }

    public bool IsArchived { get; set; }
}

public record ShortcutResponse(
    Guid Id,
    string Name,
    long AmountCents,
    Guid CategoryId,
    Guid AccountId,
    int SortOrder,
    bool IsArchived,
    DateTimeOffset CreatedAt
)
{
    public static ShortcutResponse From(Shortcut shortcut) =>
        new(
            shortcut.Id,
            shortcut.Name,
            shortcut.AmountCents,
            shortcut.CategoryId,
            shortcut.AccountId,
            shortcut.SortOrder,
            shortcut.IsArchived,
            shortcut.CreatedAt
        );
}
