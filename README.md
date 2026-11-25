# LogiMarket 360 🏪

Sistema de gestión comercial desarrollado con **.NET 8**, **Clean Architecture** y los **23 patrones de diseño GoF**, como parte de la Actividad de Aprendizaje Práctico APE-04.

---

## 🚀 Requisitos

| Herramienta | Versión mínima |
|---|---|
| .NET SDK | 8.0 |
| SQLite | Incluido vía NuGet |
| Visual Studio / VS Code | Cualquiera con soporte .NET |

---

## ▶️ Cómo ejecutar

```powershell
# 1. Clonar el repositorio
git clone https://github.com/ArielIsai/LogiMarket0360.git
cd LogiMarket0360

# 2. Restaurar dependencias
dotnet restore

# 3. Ejecutar la aplicación WinForms
dotnet run --project src/LogiMarket.WinForms

# 4. Correr la suite de pruebas
dotnet test
```

La base de datos SQLite (`logimarket.db`) se crea automáticamente en el directorio de ejecución con datos semilla.

---

## 🔐 Credenciales de demostración

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin.demo` | `admin123` | Administrador |
| `ventas.demo` | `ventas123` | Ventas |
| `bodega.demo` | `bodega123` | Bodega |
| `caja.demo` | `caja123` | Caja |

---

## 🏗️ Arquitectura

```
LogiMarket360.sln
├── src/
│   ├── LogiMarket.Domain          → Entidades, enums, excepciones, patrones de dominio
│   ├── LogiMarket.Application     → Casos de uso, DTOs, puertos, patrones de aplicación
│   ├── LogiMarket.Infrastructure  → EF Core 8 + SQLite, repositorios, servicios externos
│   └── LogiMarket.WinForms        → Interfaz gráfica, 12 vistas, Mediator, DI root
└── tests/
    └── LogiMarket.Tests           → 36 pruebas xUnit (escenarios oficiales + 23 patrones)
```

---

## 🎨 Los 23 Patrones GoF implementados

### Creacionales (5)
| # | Patrón | Archivo | Propósito de negocio |
|---|---|---|---|
| 1 | **Builder** | `Domain/Patterns/Builder/PedidoBuilder.cs` | Construye pedidos complejos línea a línea |
| 2 | **Abstract Factory** | `Application/Patterns/AbstractFactory/LogisticaAbstractFactory.cs` | Genera guías y empaques Estándar / Exprés |
| 3 | **Factory Method** | `Application/Patterns/FactoryMethod/NotificadoresFactoryMethod.cs` | Crea notificadores Email o SMS según canal |
| 4 | **Prototype** | `Domain/Patterns/Prototype/PedidoBorradorPrototipo.cs` | Clona borradores de pedido |
| 5 | **Singleton** | `Application/Patterns/Singleton/MetricasSesion.cs` | Métricas de sesión únicamente una instancia |

### Estructurales (7)
| # | Patrón | Archivo | Propósito de negocio |
|---|---|---|---|
| 6 | **Adapter** | `Infrastructure/Patterns/Adapters/PasarelasPago.cs` | Adapta pasarela legada a interfaz moderna |
| 7 | **Bridge** | `Infrastructure/Patterns/Bridge/ComprobantesBridge.cs` | Separa tipo de comprobante de su salida (PDF/TXT) |
| 8 | **Composite** | `Domain/Patterns/Composite/KitCompuesto.cs` | Árbol Producto-simple / Kit-compuesto (anti-ciclo) |
| 9 | **Decorator** | `Infrastructure/Patterns/Adapters/PasarelasPago.cs` (`AuditoriaPagoDecorator`) | Añade auditoría al cobro sin modificar la clase |
| 10 | **Facade** | `Application/Patterns/Facade/ConfirmacionPedidoFacade.cs` | Orquesta validación + cobro + despacho en un paso |
| 11 | **Flyweight** | `Infrastructure/Patterns/Flyweight/CatalogoVisualFlyweightFactory.cs` | Reutiliza metadatos visuales de productos |
| 12 | **Proxy** | `Infrastructure/Patterns/Adapters/PasarelasPago.cs` (`PagoAutorizadoProxy`) | Control de acceso y log antes de cobrar |

### Comportamentales (11)
| # | Patrón | Archivo | Propósito de negocio |
|---|---|---|---|
| 13 | **Chain of Responsibility** | `Application/Patterns/ChainOfResponsibility/` | Pipeline de validación de pedidos (cliente activo → stock → condiciones) |
| 14 | **Command** | `Application/Patterns/Command/` | Confirmar, Cancelar, Despachar, Devolver como comandos desacoplados |
| 15 | **Interpreter** | `Application/Patterns/Interpreter/FiltroPedidosInterpreter.cs` | Lenguaje de filtros: `ESTADO=Pagado AND TOTAL>50` |
| 16 | **Iterator** | `Domain/Patterns/Iterator/KitFisicoIterator.cs` | Recorre componentes físicos de un kit anidado |
| 17 | **Mediator** | `WinForms/Mediator/FormularioPrincipalMediator.cs` | Desacopla las vistas del formulario principal |
| 18 | **Memento** | `Domain/Patterns/Memento/` | Deshacer/Rehacer del carrito (máx 5 estados) |
| 19 | **Observer** | `Application/Patterns/Observer/` | Métricas + Auditoría + Notificación al pagar/despachar |
| 20 | **State** | `Domain/Patterns/State/` | Ciclo de vida del pedido: Borrador→Validado→Pagado→Despachado→Entregado |
| 21 | **Strategy** | `Domain/Patterns/Strategy/` | Descuentos: Mayorista 15% / VIP 10% / Normal 0% (no acumulables) |
| 22 | **Template Method** | `Domain/Patterns/TemplateMethod/` | Proceso de devolución: por Defecto (30d) o Arrepentimiento (7d) |
| 23 | **Visitor** | `Domain/Patterns/Visitor/` | Desglose SKU y peso logístico sin modificar entidades |

---

## 🧪 Escenarios de prueba oficiales

| Test | Escenario | Resultado esperado |
|---|---|---|
| P01 | **Ana VIP** compra Mouse+Teclado+Webcam | Total = **\$59.00** (descuento VIP 10%) |
| P02 | **Luis Normal** compra 10×Teclado | Total = **\$102.00** (Mayorista 15% priority over Normal) |
| P03 | **Carmen Mayorista** compra 10×Mouse | Total = **\$90.00** (Mayorista 15%) |
| P04 | **KIT_TELETRABAJO ×6** | Rechazado — límite 5 unidades físicas de Monitor |
| P05 | Devolución Ana VIP | Reembolso = **\$54.00** (subtotal sin envío) |

---

## 📦 Entrega

Empaquetar sin carpetas `bin/`, `obj/`, `.vs/`:

```powershell
# Desde el directorio padre de LogiMarket0360
Compress-Archive -Path "LogiMarket0360" -DestinationPath "APE04_LogiMarket360_[APELLIDOS_NOMBRES].zip" -Force
```

---

## 👤 Autor

Desarrollado como práctica académica APE-04 — Patrones de Diseño de Software  
Universidad Técnica de Ambato
