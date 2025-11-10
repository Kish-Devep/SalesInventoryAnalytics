using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesInventoryAnalytics.Persistence.Context;

namespace SalesInventoryAnalytics.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        private readonly DwhContext _context;
        private readonly ILogger<ReportesController> _logger;

        public ReportesController(DwhContext context, ILogger<ReportesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetDashboard()
        {
            try
            {
                var totalVentas = await _context.FactVentas.SumAsync(v => v.TotalVenta);
                var totalTransacciones = await _context.FactVentas.CountAsync();
                var totalClientes = await _context.DimClientes.CountAsync(c => c.EsActivo);
                var totalProductos = await _context.DimProductos.CountAsync(p => p.EsActivo);
                var productosVendidos = await _context.FactVentas.SumAsync(v => v.Cantidad);

                return Ok(new
                {
                    TotalVentas = Math.Round(totalVentas, 2),
                    TotalTransacciones = totalTransacciones,
                    PromedioVenta = Math.Round(totalTransacciones > 0 ? totalVentas / totalTransacciones : 0, 2),
                    TotalClientes = totalClientes,
                    TotalProductos = totalProductos,
                    ProductosVendidos = productosVendidos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dashboard");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("ventas-por-periodo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetVentasPorPeriodo(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            try
            {
                var fechaInicioId = int.Parse(fechaInicio.ToString("yyyyMMdd"));
                var fechaFinId = int.Parse(fechaFin.ToString("yyyyMMdd"));

                var ventas = await _context.FactVentas
                    .Where(v => v.FechaId >= fechaInicioId && v.FechaId <= fechaFinId)
                    .GroupBy(v => 1)
                    .Select(g => new
                    {
                        FechaInicio = fechaInicio.ToString("yyyy-MM-dd"),
                        FechaFin = fechaFin.ToString("yyyy-MM-dd"),
                        TotalVentas = g.Sum(v => v.TotalVenta),
                        TotalTransacciones = g.Count(),
                        PromedioVenta = g.Average(v => v.TotalVenta),
                        ProductosVendidos = g.Sum(v => v.Cantidad)
                    })
                    .FirstOrDefaultAsync();

                return Ok(ventas ?? new
                {
                    FechaInicio = fechaInicio.ToString("yyyy-MM-dd"),
                    FechaFin = fechaFin.ToString("yyyy-MM-dd"),
                    TotalVentas = 0m,
                    TotalTransacciones = 0,
                    PromedioVenta = 0m,
                    ProductosVendidos = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas por periodo");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("ventas-por-ubicacion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetVentasPorUbicacion(
            [FromQuery] string? pais = null,
            [FromQuery] string? ciudad = null)
        {
            try
            {
                var query = _context.FactVentas
                    .Include(v => v.Cliente)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pais))
                    query = query.Where(v => v.Cliente.Pais == pais);

                if (!string.IsNullOrEmpty(ciudad))
                    query = query.Where(v => v.Cliente.Ciudad == ciudad);

                var result = await query
                    .GroupBy(v => new { v.Cliente.Pais, v.Cliente.Ciudad })
                    .Select(g => new
                    {
                        Pais = g.Key.Pais,
                        Ciudad = g.Key.Ciudad,
                        TotalVentas = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        CantidadTransacciones = g.Count(),
                        ClientesUnicos = g.Select(v => v.ClienteId).Distinct().Count(),
                        PromedioVenta = Math.Round(g.Average(v => v.TotalVenta), 2)
                    })
                    .OrderByDescending(x => x.TotalVentas)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas por ubicación");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }


        [HttpGet("productos-mas-vendidos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetProductosMasVendidos([FromQuery] int top = 10)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Producto)
                    .GroupBy(v => new { v.ProductoId, v.Producto.NombreProducto, v.Producto.Categoria })
                    .Select(g => new
                    {
                        ProductoId = g.Key.ProductoId,
                        NombreProducto = g.Key.NombreProducto,
                        Categoria = g.Key.Categoria,
                        CantidadVendida = g.Sum(v => v.Cantidad),
                        TotalIngresos = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        NumeroTransacciones = g.Count(),
                        PrecioPromedio = Math.Round(g.Average(v => v.PrecioUnitario), 2)
                    })
                    .OrderByDescending(x => x.CantidadVendida)
                    .Take(top)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos más vendidos");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("productos-mayor-ingreso")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetProductosMayorIngreso([FromQuery] int top = 10)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Producto)
                    .GroupBy(v => new { v.ProductoId, v.Producto.NombreProducto, v.Producto.Categoria })
                    .Select(g => new
                    {
                        ProductoId = g.Key.ProductoId,
                        NombreProducto = g.Key.NombreProducto,
                        Categoria = g.Key.Categoria,
                        TotalIngresos = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        CantidadVendida = g.Sum(v => v.Cantidad),
                        NumeroTransacciones = g.Count()
                    })
                    .OrderByDescending(x => x.TotalIngresos)
                    .Take(top)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos con mayor ingreso");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("productos-baja-rotacion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetProductosBajaRotacion([FromQuery] int top = 10)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Producto)
                    .GroupBy(v => new { v.ProductoId, v.Producto.NombreProducto, v.Producto.Categoria, v.Producto.StockActual })
                    .Select(g => new
                    {
                        ProductoId = g.Key.ProductoId,
                        NombreProducto = g.Key.NombreProducto,
                        Categoria = g.Key.Categoria,
                        StockActual = g.Key.StockActual,
                        CantidadVendida = g.Sum(v => v.Cantidad),
                        TotalIngresos = Math.Round(g.Sum(v => v.TotalVenta), 2)
                    })
                    .OrderBy(x => x.CantidadVendida)
                    .Take(top)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos de baja rotación");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("evolucion-producto/{productoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetEvolucionProducto(int productoId)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Fecha)
                    .Include(v => v.Producto)
                    .Where(v => v.ProductoId == productoId)
                    .GroupBy(v => new { v.Fecha.Anio, v.Fecha.Mes, v.Fecha.NombreMes })
                    .Select(g => new
                    {
                        Anio = g.Key.Anio,
                        Mes = g.Key.Mes,
                        NombreMes = g.Key.NombreMes,
                        CantidadVendida = g.Sum(v => v.Cantidad),
                        TotalVentas = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        NumeroTransacciones = g.Count()
                    })
                    .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener evolución del producto");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("ventas-por-categoria")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetVentasPorCategoria()
        {
            try
            {
                var totalGeneral = await _context.FactVentas.SumAsync(v => v.TotalVenta);

                var result = await _context.FactVentas
                    .Include(v => v.Producto)
                    .GroupBy(v => v.Producto.Categoria)
                    .Select(g => new
                    {
                        Categoria = g.Key,
                        TotalVentas = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        CantidadVendida = g.Sum(v => v.Cantidad),
                        NumeroTransacciones = g.Count(),
                        PorcentajeDelTotal = Math.Round((g.Sum(v => v.TotalVenta) / totalGeneral) * 100, 2)
                    })
                    .OrderByDescending(x => x.TotalVentas)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas por categoría");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("clientes-mas-compras")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetClientesMasCompras([FromQuery] int top = 10)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Cliente)
                    .GroupBy(v => new { v.ClienteId, v.Cliente.Nombre, v.Cliente.Apellido, v.Cliente.Pais, v.Cliente.Ciudad })
                    .Select(g => new
                    {
                        ClienteId = g.Key.ClienteId,
                        NombreCompleto = $"{g.Key.Nombre} {g.Key.Apellido}",
                        Pais = g.Key.Pais,
                        Ciudad = g.Key.Ciudad,
                        TotalCompras = g.Count(),
                        TotalGastado = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        PromedioCompra = Math.Round(g.Average(v => v.TotalVenta), 2),
                        ProductosComprados = g.Sum(v => v.Cantidad)
                    })
                    .OrderByDescending(x => x.TotalCompras)
                    .Take(top)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes con más compras");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("clientes-mayor-ingreso")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetClientesMayorIngreso([FromQuery] int top = 10)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Cliente)
                    .GroupBy(v => new { v.ClienteId, v.Cliente.Nombre, v.Cliente.Apellido, v.Cliente.Pais })
                    .Select(g => new
                    {
                        ClienteId = g.Key.ClienteId,
                        NombreCompleto = $"{g.Key.Nombre} {g.Key.Apellido}",
                        Pais = g.Key.Pais,
                        TotalGastado = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        TotalCompras = g.Count(),
                        PromedioCompra = Math.Round(g.Average(v => v.TotalVenta), 2)
                    })
                    .OrderByDescending(x => x.TotalGastado)
                    .Take(top)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes con mayor ingreso");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("promedio-productos-por-cliente")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetPromedioProductosPorCliente()
        {
            try
            {
                var promedio = await _context.FactVentas
                    .GroupBy(v => v.ClienteId)
                    .Select(g => new
                    {
                        ProductosPorTransaccion = (double)g.Sum(v => v.Cantidad) / g.Count()
                    })
                    .AverageAsync(x => x.ProductosPorTransaccion);

                return Ok(new
                {
                    PromedioProductosPorTransaccion = Math.Round(promedio, 2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener promedio de productos por cliente");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("top5-clientes-porcentaje")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetTop5ClientesPorcentaje()
        {
            try
            {
                var totalGeneral = await _context.FactVentas.SumAsync(v => v.TotalVenta);

                var top5 = await _context.FactVentas
                    .Include(v => v.Cliente)
                    .GroupBy(v => new { v.ClienteId, v.Cliente.Nombre, v.Cliente.Apellido })
                    .Select(g => new
                    {
                        NombreCompleto = $"{g.Key.Nombre} {g.Key.Apellido}",
                        TotalGastado = g.Sum(v => v.TotalVenta)
                    })
                    .OrderByDescending(x => x.TotalGastado)
                    .Take(5)
                    .ToListAsync();

                var totalTop5 = top5.Sum(x => x.TotalGastado);
                var porcentaje = (totalTop5 / totalGeneral) * 100;

                return Ok(new
                {
                    TotalVentasGeneral = Math.Round(totalGeneral, 2),
                    TotalVentasTop5 = Math.Round(totalTop5, 2),
                    PorcentajeQueRepresentan = Math.Round(porcentaje, 2),
                    Top5Clientes = top5.Select(x => new
                    {
                        x.NombreCompleto,
                        TotalGastado = Math.Round(x.TotalGastado, 2),
                        PorcentajeDelTotal = Math.Round((x.TotalGastado / totalGeneral) * 100, 2)
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener top 5 clientes porcentaje");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }


        [HttpGet("ventas-por-mes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetVentasPorMes([FromQuery] int? anio = null)
        {
            try
            {
                var query = _context.FactVentas
                    .Include(v => v.Fecha)
                    .AsQueryable();

                if (anio.HasValue)
                {
                    query = query.Where(v => v.Fecha.Anio == anio.Value);
                }

                var result = await query
                    .GroupBy(v => new { v.Fecha.Anio, v.Fecha.Mes, v.Fecha.NombreMes })
                    .Select(g => new
                    {
                        Anio = g.Key.Anio,
                        Mes = g.Key.Mes,
                        NombreMes = g.Key.NombreMes,
                        TotalVentas = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        CantidadTransacciones = g.Count(),
                        PromedioVenta = Math.Round(g.Average(v => v.TotalVenta), 2),
                        ProductosVendidos = g.Sum(v => v.Cantidad)
                    })
                    .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas por mes");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }


        [HttpGet("picos-de-ventas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetPicosDeVentas([FromQuery] int top = 5)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Fecha)
                    .GroupBy(v => new { v.Fecha.Anio, v.Fecha.Mes, v.Fecha.NombreMes })
                    .Select(g => new
                    {
                        Anio = g.Key.Anio,
                        Mes = g.Key.Mes,
                        NombreMes = g.Key.NombreMes,
                        TotalVentas = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        CantidadTransacciones = g.Count()
                    })
                    .OrderByDescending(x => x.TotalVentas)
                    .Take(top)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener picos de ventas");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("evolucion-anual")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetEvolucionAnual([FromQuery] int anio)
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Fecha)
                    .Where(v => v.Fecha.Anio == anio)
                    .GroupBy(v => new { v.Fecha.Mes, v.Fecha.NombreMes })
                    .Select(g => new
                    {
                        Mes = g.Key.Mes,
                        NombreMes = g.Key.NombreMes,
                        TotalVentas = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        CantidadTransacciones = g.Count()
                    })
                    .OrderBy(x => x.Mes)
                    .ToListAsync();

                decimal acumulado = 0;
                var resultadoConAcumulado = result.Select(x =>
                {
                    acumulado += x.TotalVentas;
                    return new
                    {
                        x.Mes,
                        x.NombreMes,
                        x.TotalVentas,
                        x.CantidadTransacciones,
                        VentasAcumuladas = Math.Round(acumulado, 2)
                    };
                });

                return Ok(resultadoConAcumulado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener evolución anual");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("comparativa-anual")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetComparativaAnual([FromQuery] int anio1, [FromQuery] int anio2)
        {
            try
            {
                var ventasAnio1 = await _context.FactVentas
                    .Include(v => v.Fecha)
                    .Where(v => v.Fecha.Anio == anio1)
                    .SumAsync(v => v.TotalVenta);

                var ventasAnio2 = await _context.FactVentas
                    .Include(v => v.Fecha)
                    .Where(v => v.Fecha.Anio == anio2)
                    .SumAsync(v => v.TotalVenta);

                var diferencia = ventasAnio2 - ventasAnio1;
                var crecimiento = ventasAnio1 > 0 ? (diferencia / ventasAnio1) * 100 : 0;

                return Ok(new
                {
                    Anio1 = anio1,
                    VentasAnio1 = Math.Round(ventasAnio1, 2),
                    Anio2 = anio2,
                    VentasAnio2 = Math.Round(ventasAnio2, 2),
                    Diferencia = Math.Round(diferencia, 2),
                    CrecimientoPorcentual = Math.Round(crecimiento, 2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener comparativa anual");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("ventas-por-pais")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetVentasPorPais()
        {
            try
            {
                var result = await _context.FactVentas
                    .Include(v => v.Cliente)
                    .GroupBy(v => v.Cliente.Pais)
                    .Select(g => new
                    {
                        Pais = g.Key,
                        TotalVentas = Math.Round(g.Sum(v => v.TotalVenta), 2),
                        CantidadTransacciones = g.Count(),
                        ClientesUnicos = g.Select(v => v.ClienteId).Distinct().Count(),
                        PromedioVenta = Math.Round(g.Average(v => v.TotalVenta), 2)
                    })
                    .OrderByDescending(x => x.TotalVentas)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas por país");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("crecimiento-mensual")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<object>>> GetCrecimientoMensual([FromQuery] int anio)
        {
            try
            {
                var ventasMensuales = await _context.FactVentas
                    .Include(v => v.Fecha)
                    .Where(v => v.Fecha.Anio == anio)
                    .GroupBy(v => new { v.Fecha.Mes, v.Fecha.NombreMes })
                    .Select(g => new
                    {
                        Mes = g.Key.Mes,
                        NombreMes = g.Key.NombreMes,
                        TotalVentas = g.Sum(v => v.TotalVenta)
                    })
                    .OrderBy(x => x.Mes)
                    .ToListAsync();

                var resultado = ventasMensuales.Select((x, index) =>
                {
                    decimal? crecimiento = null;
                    if (index > 0)
                    {
                        var ventasMesAnterior = ventasMensuales[index - 1].TotalVentas;
                        if (ventasMesAnterior > 0)
                        {
                            crecimiento = ((x.TotalVentas - ventasMesAnterior) / ventasMesAnterior) * 100;
                        }
                    }

                    return new
                    {
                        x.Mes,
                        x.NombreMes,
                        TotalVentas = Math.Round(x.TotalVentas, 2),
                        CrecimientoVsMesAnterior = crecimiento.HasValue ? Math.Round(crecimiento.Value, 2) : (decimal?)null
                    };
                }).ToList();

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener crecimiento mensual");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }
    }
}