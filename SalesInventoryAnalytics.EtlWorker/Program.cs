using Microsoft.EntityFrameworkCore;
using Serilog;
using SalesInventoryAnalytics.Application.Services.ETL;
using SalesInventoryAnalytics.Domain.Entities.SourceData.Csv;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using SalesInventoryAnalytics.Domain.Interfaces.ETL;
using SalesInventoryAnalytics.Domain.Interfaces.Repositories;
using SalesInventoryAnalytics.EtlWorker.Workers;
using SalesInventoryAnalytics.Infrastructure.ETL.Extractors;
using SalesInventoryAnalytics.Persistence.Context;
using SalesInventoryAnalytics.Persistence.Repositories;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/etl-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando ETL Worker Service...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    builder.Services.AddDbContext<StagingContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("StagingConnection")));

    builder.Services.AddDbContext<DwhContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DwhConnection")));

    builder.Services.AddHttpClient();

    // CSV Extractors
    builder.Services.AddScoped<IExtractor<CustomerCsv>, CsvExtractor<CustomerCsv>>();
    builder.Services.AddScoped<IExtractor<ProductCsv>, CsvExtractor<ProductCsv>>();
    builder.Services.AddScoped<IExtractor<OrderCsv>, CsvExtractor<OrderCsv>>();
    builder.Services.AddScoped<IExtractor<OrderDetailCsv>, CsvExtractor<OrderDetailCsv>>();

    // Database Extractor
    builder.Services.AddScoped<IExtractor<StagingSale>, DatabaseExtractor>();

    builder.Services.AddScoped<CustomerTransformerService>();
    builder.Services.AddScoped<ProductTransformerService>();
    builder.Services.AddScoped<SaleTransformerService>();


    builder.Services.AddScoped<IStagingRepository, StagingRepository>();
    builder.Services.AddScoped<IDwhRepository, DwhRepository>();


    builder.Services.AddScoped<DwhLoaderService>();

    builder.Services.AddHostedService<EtlBackgroundService>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El Worker Service falló al iniciar");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;