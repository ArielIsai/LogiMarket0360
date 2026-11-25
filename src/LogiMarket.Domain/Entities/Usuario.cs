using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Usuario() { }

    public Usuario(string username, string passwordHash, string salt, RolUsuario rol, string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("El nombre de usuario es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("El hash de contraseña es obligatorio.");

        Username = username.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Salt = salt;
        Rol = rol;
        NombreCompleto = nombreCompleto.Trim();
    }
}
