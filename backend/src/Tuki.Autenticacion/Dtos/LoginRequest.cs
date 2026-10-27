using System.ComponentModel.DataAnnotations;

namespace Tuki.Autenticacion.Dtos;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
