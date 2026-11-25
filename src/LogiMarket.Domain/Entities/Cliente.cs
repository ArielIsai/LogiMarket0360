using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public CategoriaCliente Categoria { get; set; } = CategoriaCliente.Normal;
    public string Contacto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Cliente() { }

    public Cliente(string codigo, string nombre, string identificacion, CategoriaCliente categoria, string contacto, string correo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new DomainException("El código de cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del cliente es obligatorio.");

        Codigo = codigo.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Identificacion = identificacion.Trim();
        Categoria = categoria;
        Contacto = contacto.Trim();
        Correo = correo.Trim();
    }
}
