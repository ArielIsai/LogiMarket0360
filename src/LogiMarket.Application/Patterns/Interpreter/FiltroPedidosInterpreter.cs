using System.Globalization;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Patterns.Interpreter;

public interface IExpresionFiltro
{
    bool Interpretar(Pedido pedido);
}

// Expresión Terminal para ESTADO
public class EstadoExpresion : IExpresionFiltro
{
    private readonly EstadoPedidoEnum _estadoEsperado;

    public EstadoExpresion(string valor)
    {
        if (Enum.TryParse<EstadoPedidoEnum>(valor.Trim(), true, out var estado))
            _estadoEsperado = estado;
        else
            throw new FormatException($"Estado desconocido en la consulta: '{valor}'. Estados válidos: BORRADOR, VALIDADO, PAGADO, DESPACHADO, ENTREGADO, CANCELADO.");
    }

    public bool Interpretar(Pedido pedido) => pedido.Estado == _estadoEsperado;
}

// Expresión Terminal para TOTAL (con operadores =, >=, <=, >, <)
public class TotalExpresion : IExpresionFiltro
{
    private readonly string _operador;
    private readonly decimal _valor;

    public TotalExpresion(string operador, string valor)
    {
        _operador = operador;
        if (!decimal.TryParse(valor.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out _valor) &&
            !decimal.TryParse(valor.Trim(), out _valor))
        {
            throw new FormatException($"Valor numérico no válido para TOTAL: '{valor}'.");
        }
    }

    public bool Interpretar(Pedido pedido)
    {
        return _operador switch
        {
            "=" => pedido.Total == _valor,
            ">=" => pedido.Total >= _valor,
            "<=" => pedido.Total <= _valor,
            ">" => pedido.Total > _valor,
            "<" => pedido.Total < _valor,
            _ => throw new InvalidOperationException($"Operador '{_operador}' no soportado para TOTAL.")
        };
    }
}

// Expresión Terminal para CLIENTE (= o LIKE)
public class ClienteExpresion : IExpresionFiltro
{
    private readonly string _nombreEsperado;

    public ClienteExpresion(string valor)
    {
        _nombreEsperado = valor.Trim().Trim('\'', '"').ToLowerInvariant();
    }

    public bool Interpretar(Pedido pedido)
    {
        var nombre = pedido.Cliente?.Nombre?.ToLowerInvariant() ?? string.Empty;
        return nombre.Contains(_nombreEsperado);
    }
}

// Expresiones No Terminales
public class AndExpresion : IExpresionFiltro
{
    private readonly IExpresionFiltro _izq;
    private readonly IExpresionFiltro _der;

    public AndExpresion(IExpresionFiltro izq, IExpresionFiltro der)
    {
        _izq = izq;
        _der = der;
    }

    public bool Interpretar(Pedido pedido) => _izq.Interpretar(pedido) && _der.Interpretar(pedido);
}

public class OrExpresion : IExpresionFiltro
{
    private readonly IExpresionFiltro _izq;
    private readonly IExpresionFiltro _der;

    public OrExpresion(IExpresionFiltro izq, IExpresionFiltro der)
    {
        _izq = izq;
        _der = der;
    }

    public bool Interpretar(Pedido pedido) => _izq.Interpretar(pedido) || _der.Interpretar(pedido);
}

// Parser descendente recursivo con soporte para precedencia y paréntesis
public class ParserFiltroPedidos
{
    private readonly List<string> _tokens;
    private int _pos;

    public ParserFiltroPedidos(string consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta))
            throw new ArgumentException("La consulta de filtro no puede estar vacía.");

        _tokens = Tokenizar(consulta);
        _pos = 0;
    }

    private static List<string> Tokenizar(string texto)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < texto.Length)
        {
            if (char.IsWhiteSpace(texto[i]))
            {
                i++;
                continue;
            }

            if (texto[i] == '(' || texto[i] == ')')
            {
                tokens.Add(texto[i].ToString());
                i++;
                continue;
            }

            if (texto[i] == '>' || texto[i] == '<' || texto[i] == '=')
            {
                if (i + 1 < texto.Length && texto[i + 1] == '=')
                {
                    tokens.Add(texto.Substring(i, 2));
                    i += 2;
                }
                else
                {
                    tokens.Add(texto[i].ToString());
                    i++;
                }
                continue;
            }

            // Palabras o valores entre comillas
            if (texto[i] == '\'' || texto[i] == '"')
            {
                char quote = texto[i++];
                int inicio = i;
                while (i < texto.Length && texto[i] != quote) i++;
                tokens.Add(texto.Substring(inicio, i - inicio));
                if (i < texto.Length) i++; // saltar comilla de cierre
                continue;
            }

            int start = i;
            while (i < texto.Length && !char.IsWhiteSpace(texto[i]) && texto[i] != '(' && texto[i] != ')' && texto[i] != '>' && texto[i] != '<' && texto[i] != '=')
            {
                i++;
            }
            tokens.Add(texto.Substring(start, i - start));
        }

        return tokens;
    }

    public IExpresionFiltro Parsear()
    {
        var expr = ParseOr();
        if (_pos < _tokens.Count)
        {
            throw new FormatException($"Error de sintaxis: Token inesperado '{_tokens[_pos]}' en posición {_pos}.");
        }
        return expr;
    }

    private IExpresionFiltro ParseOr()
    {
        var expr = ParseAnd();
        while (_pos < _tokens.Count && _tokens[_pos].Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            _pos++;
            var der = ParseAnd();
            expr = new OrExpresion(expr, der);
        }
        return expr;
    }

    private IExpresionFiltro ParseAnd()
    {
        var expr = ParseFactor();
        while (_pos < _tokens.Count && _tokens[_pos].Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            _pos++;
            var der = ParseFactor();
            expr = new AndExpresion(expr, der);
        }
        return expr;
    }

    private IExpresionFiltro ParseFactor()
    {
        if (_pos >= _tokens.Count)
            throw new FormatException("Error de sintaxis: Expresión incompleta o vacía.");

        if (_tokens[_pos] == "(")
        {
            _pos++;
            var expr = ParseOr();
            if (_pos >= _tokens.Count || _tokens[_pos] != ")")
                throw new FormatException("Error de sintaxis: Falta paréntesis de cierre ')'.");
            _pos++;
            return expr;
        }

        // Caso comparación: CAMPO OPERADOR VALOR
        if (_pos + 2 >= _tokens.Count)
            throw new FormatException($"Error de sintaxis cerca de '{_tokens[_pos]}'. Se esperaba 'CAMPO OPERADOR VALOR'.");

        string campo = _tokens[_pos++].ToUpperInvariant();
        string op = _tokens[_pos++];
        string valor = _tokens[_pos++];

        return campo switch
        {
            "ESTADO" => op == "=" ? new EstadoExpresion(valor) : throw new FormatException($"El campo ESTADO solo admite el operador '='. Recibido: '{op}'."),
            "TOTAL" => new TotalExpresion(op, valor),
            "CLIENTE" => (op == "=" || op.Equals("LIKE", StringComparison.OrdinalIgnoreCase)) ? new ClienteExpresion(valor) : throw new FormatException($"El campo CLIENTE solo admite '=' o 'LIKE'. Recibido: '{op}'."),
            _ => throw new FormatException($"Campo desconocido en la consulta: '{campo}'. Campos admitidos: ESTADO, TOTAL, CLIENTE.")
        };
    }
}
