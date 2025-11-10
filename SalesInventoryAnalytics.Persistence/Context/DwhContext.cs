using Microsoft.EntityFrameworkCore;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;

namespace SalesInventoryAnalytics.Persistence.Context
{
    public class DwhContext : DbContext
    {
        public DwhContext(DbContextOptions<DwhContext> options) : base(options)
        {
        }

        // Dimensiones
        public DbSet<DimCliente> DimClientes { get; set; }
        public DbSet<DimProducto> DimProductos { get; set; }
        public DbSet<DimFecha> DimFechas { get; set; }

        // Facts
        public DbSet<FactVentas> FactVentas { get; set; }

        #region Basic and Property configs
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===================================
            // Dimensión: Dim_Cliente
            // ===================================
            modelBuilder.Entity<DimCliente>(entity =>
            {
                entity.ToTable("Dim_Cliente");
                entity.HasKey(e => e.ClienteId);

                entity.Property(e => e.CodigoCliente)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Apellido)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .HasMaxLength(255);

                entity.Property(e => e.Telefono)
                    .HasMaxLength(100);

                entity.Property(e => e.Ciudad)
                    .HasMaxLength(100);

                entity.Property(e => e.Pais)
                    .HasMaxLength(100);

                entity.Property(e => e.Version)
                    .HasDefaultValue(1);

                entity.Property(e => e.EsActivo)
                    .HasDefaultValue(true);

                entity.Property(e => e.FechaInicioValidez)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                // Index de este
                entity.HasIndex(e => new { e.CodigoCliente, e.EsActivo });
                entity.HasIndex(e => e.Pais);
            });

            // ===================================
            // Dimensión: Dim_Producto
            // ===================================
            modelBuilder.Entity<DimProducto>(entity =>
            {
                entity.ToTable("Dim_Producto");
                entity.HasKey(e => e.ProductoId);

                entity.Property(e => e.CodigoProducto)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.NombreProducto)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Categoria)
                    .HasMaxLength(100);

                entity.Property(e => e.PrecioUnitario)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.StockActual)
                    .IsRequired();

                entity.Property(e => e.Version)
                    .HasDefaultValue(1);

                entity.Property(e => e.EsActivo)
                    .HasDefaultValue(true);

                entity.Property(e => e.FechaInicioValidez)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                // Índices
                entity.HasIndex(e => new { e.CodigoProducto, e.EsActivo });
                entity.HasIndex(e => e.Categoria);
            });

            // ===================================
            // Dimensión: Dim_Fecha
            // ===================================
            modelBuilder.Entity<DimFecha>(entity =>
            {
                entity.ToTable("Dim_Fecha");
                entity.HasKey(e => e.FechaId);

                entity.Property(e => e.FechaId)
                    .ValueGeneratedNever();

                entity.Property(e => e.Fecha)
                    .IsRequired();

                entity.Property(e => e.NombreMes)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.NombreDia)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                // Índices
                entity.HasIndex(e => e.Fecha).IsUnique();
                entity.HasIndex(e => new { e.Anio, e.Mes });
            });

            // ===================================
            // Tabla de Hechos: Fact_Ventas
            // ===================================
            modelBuilder.Entity<FactVentas>(entity =>
            {
                entity.ToTable("Fact_Ventas");
                entity.HasKey(e => e.VentaId);

                entity.Property(e => e.NumeroOrden)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Estado)
                    .HasMaxLength(50);

                entity.Property(e => e.Cantidad)
                    .IsRequired();

                entity.Property(e => e.PrecioUnitario)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.TotalVenta)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.OrigenDatos)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Ventas)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Producto)
                    .WithMany(p => p.Ventas)
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Fecha)
                    .WithMany(f => f.Ventas)
                    .HasForeignKey(e => e.FechaId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index para optimización de queries analíticas
                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.ProductoId);
                entity.HasIndex(e => e.FechaId);
                entity.HasIndex(e => e.NumeroOrden);
                entity.HasIndex(e => new { e.FechaId, e.ClienteId, e.ProductoId });
            });
        }
        #endregion
    }
}