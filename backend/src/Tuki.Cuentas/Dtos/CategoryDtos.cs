using System.ComponentModel.DataAnnotations;
using Tuki.Cuentas.Models;

namespace Tuki.Cuentas.Dtos;

public class CreateCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public CategoryKind Kind { get; set; }

    // Vacío = categoría principal; con valor = subcategoría (un solo nivel)
    public Guid? ParentId { get; set; }
}

// El PUT reemplaza la categoría; IsArchived permite restaurar una dada de baja
public class UpdateCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public CategoryKind Kind { get; set; }

    public Guid? ParentId { get; set; }

    public bool IsArchived { get; set; }
}

public record CategoryResponse(
    Guid Id,
    Guid? ParentId,
    string Name,
    CategoryKind Kind,
    bool IsArchived,
    DateTimeOffset CreatedAt
)
{
    public static CategoryResponse From(Category category) =>
        new(
            category.Id,
            category.ParentId,
            category.Name,
            category.Kind,
            category.IsArchived,
            category.CreatedAt
        );
}
