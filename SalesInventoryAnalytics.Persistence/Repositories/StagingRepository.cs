using Microsoft.EntityFrameworkCore;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using SalesInventoryAnalytics.Domain.Interfaces.Repositories;
using SalesInventoryAnalytics.Persistence.Context;

namespace SalesInventoryAnalytics.Persistence.Repositories
{
    public class StagingRepository : IStagingRepository
    {
        private readonly StagingContext _context;

        public StagingRepository(StagingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<StagingCustomer>> GetUnprocessedCustomersAsync()
        {
            return await _context.StagingCustomers
                .Where(x => x.EsValido && !x.Procesado)
                .ToListAsync();
        }

        public async Task MarkCustomersAsProcessedAsync(IEnumerable<StagingCustomer> customers)
        {
            foreach (var customer in customers)
            {
                customer.Procesado = true;
                customer.FechaActualizacion = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<StagingProduct>> GetUnprocessedProductsAsync()
        {
            return await _context.StagingProducts
                .Where(x => x.EsValido && !x.Procesado)
                .ToListAsync();
        }

        public async Task MarkProductsAsProcessedAsync(IEnumerable<StagingProduct> products)
        {
            foreach (var product in products)
            {
                product.Procesado = true;
                product.FechaActualizacion = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<StagingSale>> GetUnprocessedSalesAsync()
        {
            return await _context.StagingSales
                .Where(x => x.EsValido && !x.Procesado)
                .ToListAsync();
        }

        public async Task MarkSalesAsProcessedAsync(IEnumerable<StagingSale> sales)
        {
            foreach (var sale in sales)
            {
                sale.Procesado = true;
                sale.FechaActualizacion = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSalesValidationAsync(IEnumerable<StagingSale> sales)
        {
            await _context.SaveChangesAsync();
        }
    }
}