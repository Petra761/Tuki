using System.ComponentModel.DataAnnotations;

namespace Tuki.Administrador.Dtos;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
