using LogiMarket.Application.DTOs;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Ports;

public interface IPasarelaPago
{
    string NombreProveedor { get; }
    Task<ResultadoPagoDto> CobrarAsync(decimal monto, string claveIdempotencia, string usuarioRol);
}

public interface INotificador
{
    TipoAvisoEnum Tipo { get; }
    Task EnviarAvisoAsync(string destinatario, string mensaje, string asunto);
}

public interface ISalidaDocumento
{
    string Formato { get; }
    string EmitirDocumento(string titulo, string contenido, string rutaDestino);
}

public interface ISesionContext
{
    Usuario? UsuarioActual { get; }
    bool EstaAutenticado => UsuarioActual != null;
    RolUsuario RolActual => UsuarioActual?.Rol ?? RolUsuario.Consulta;
    void IniciarSesion(Usuario usuario);
    void CerrarSesion();
}
