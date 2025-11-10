using SalesInventoryAnalytics.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Entities.Staging
{
    // Staging table para clientes.
    public class StagingCustomer : BaseEntity
    {
        public int StagingCustomerId { get; set; }
        public string CodigoCliente { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;

        // Campos de control ETL
        public string OrigenDatos { get; set; } = string.Empty; // Bien sea CSV, API, DB
        public bool EsValido { get; set; } = false;
        public string? ErrorValidacion { get; set; }
        public bool Procesado { get; set; } = false;
    }
}
