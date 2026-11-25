using System.Security.Cryptography;
using System.Text;
using LogiMarket.Application.DTOs;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Services;

public class AuthService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ISesionContext _sesionContext;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUsuarioRepository usuarioRepo,
        ISesionContext sesionContext,
        IAuditoriaRepository auditoriaRepo,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepo = usuarioRepo;
        _sesionContext = sesionContext;
        _auditoriaRepo = auditoriaRepo;
        _unitOfWork = unitOfWork;
    }

    public static (string Hash, string Salt) GenerarHash(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        string salt = Convert.ToBase64String(saltBytes);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
        return (hash, salt);
    }

    public static bool VerificarPassword(string password, string hashGuardado, string saltGuardado)
    {
        byte[] saltBytes = Convert.FromBase64String(saltGuardado);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        string hashCalculado = Convert.ToBase64String(pbkdf2.GetBytes(32));
        return hashCalculado == hashGuardado;
    }

    public async Task<ResultadoOperacionDto> IniciarSesionAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new ResultadoOperacionDto(false, "Debe ingresar usuario y contraseña.");

        var usuario = await _usuarioRepo.GetByUsernameAsync(username.Trim().ToLowerInvariant());
        if (usuario == null || !usuario.Activo)
            return new ResultadoOperacionDto(false, "Credenciales incorrectas o usuario inactivo.");

        if (!VerificarPassword(password, usuario.PasswordHash, usuario.Salt))
            return new ResultadoOperacionDto(false, "Credenciales incorrectas.");

        _sesionContext.IniciarSesion(usuario);

        await _auditoriaRepo.AddAsync(new Auditoria(
            usuario.Username, "InicioSesion", "Usuario", usuario.Id.ToString(), $"Inicio de sesión exitoso con rol {usuario.Rol}."
        ));
        await _unitOfWork.CommitAsync();

        return new ResultadoOperacionDto(true, $"Bienvenido {usuario.NombreCompleto}", usuario);
    }

    public void CerrarSesion()
    {
        _sesionContext.CerrarSesion();
    }
}
