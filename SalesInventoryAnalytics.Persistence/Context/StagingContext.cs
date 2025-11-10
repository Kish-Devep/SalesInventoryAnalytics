using Microsoft.EntityFrameworkCore;
using SalesInventoryAnalytics.Domain.Entities.Staging;

namespace SalesInventoryAnalytics.Persistence.Context
{
    public class StagingContext : DbContext
    {
        public StagingContext(DbContextOptions<StagingContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<StagingCustomer> StagingCustomers { get; set; }
        public DbSet<StagingProduct> StagingProducts { get; set; }
        public DbSet<StagingSale> StagingSales { get; set; }

        #region Basic and Property configs
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Config de Fluent API para Staging_Customer
            modelBuilder.Entity<StagingCustomer>(entity =>
            {
                entity.ToTable("Staging_Customer");
                entity.HasKey(e => e.StagingCustomerId);

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

                entity.Property(e => e.OrigenDatos)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                // Index
                entity.HasIndex(e => e.CodigoCliente);
                entity.HasIndex(e => e.Procesado);
            });

            // Config de Fluent API para Staging_Product
            modelBuilder.Entity<StagingProduct>(entity =>
            {
                entity.ToTable("Staging_Product");
                entity.HasKey(e => e.StagingProductId);

                entity.Property(e => e.CodigoProducto)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.NombreProducto)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Categoria)
                    .HasMaxLength(100);

                entity.Property(e => e.PrecioUnitario)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.OrigenDatos)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                // Index
                entity.HasIndex(e => e.CodigoProducto);
                entity.HasIndex(e => e.Procesado);
            });

            // Config de Fluent API para Staging_Sale
            modelBuilder.Entity<StagingSale>(entity =>
            {
                entity.ToTable("Staging_Sale");
                entity.HasKey(e => e.StagingSaleId);

                entity.Property(e => e.NumeroOrden)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CodigoCliente)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CodigoProducto)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Estado)
                    .HasMaxLength(50);

                entity.Property(e => e.PrecioUnitario)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.TotalVenta)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.OrigenDatos)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                // Index
                entity.HasIndex(e => e.NumeroOrden);
                entity.HasIndex(e => e.CodigoCliente);
                entity.HasIndex(e => e.CodigoProducto);
                entity.HasIndex(e => e.Procesado);
            });
        }
        #endregion
    }
}