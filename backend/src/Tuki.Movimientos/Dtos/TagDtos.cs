using System.ComponentModel.DataAnnotations;
using Tuki.Movimientos.Models;

namespace Tuki.Movimientos.Dtos;

public class CreateTagRequest
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}

public record TagResponse(Guid Id, string Name, bool IsArchived, DateTimeOffset CreatedAt)
{
    public static TagResponse From(Tag tag) => new(tag.Id, tag.Name, tag.IsArchived, tag.CreatedAt);
}
