using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoloniAPI.Services.Utils;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.Moloni.Services.Cryptography;
using Nop.Plugin.Misc.Moloni.Services.IsolatedStorage;
using Nop.Plugin.Misc.Moloni.Services.MoloniCategoryService;
using Nop.Plugin.Misc.Moloni.Services.MoloniClientService;
using Nop.Plugin.Misc.Moloni.Services.MoloniInvoiceReceiptService;
using Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices;
using Nop.Plugin.Misc.Moloni.Services.MoloniOrderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniPaymentService;
using Nop.Plugin.Misc.Moloni.Services.MoloniProductService;
using Nop.Plugin.Misc.Moloni.Services.MoloniReturnsService;
using Nop.Plugin.Misc.Moloni.Services.MoloniSetsService;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Services.MoloniStockService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTaxService;
using Nop.Plugin.Misc.Moloni.Services.MoloniTokenService;
using Nop.Plugin.Misc.Moloni.Services.MoloniWarehouseService;
using Nop.Plugin.Misc.Moloni.Services.SubscriptionService;

namespace Nop.Plugin.Misc.Moloni.Infrastructure;

/// <summary>
/// Represents object for the configuring services on application startup
/// </summary>
public class NopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIsolatedStorage, IsolatedStorage>();
        services.AddScoped<ICryptography, Cryptography>();
        services.AddScoped<IUtils, Utils>();
        services.AddScoped<IMoloniPaymentService, MoloniPaymentService>();
        services.AddScoped<IMoloniClientService, MoloniClientService>();
        services.AddScoped<IMoloniTokenService, MoloniTokenService>();
        services.AddScoped<IMoloniOrderService, MoloniOrderService>();
        services.AddScoped<IMoloniProductService, MoloniProductService>();
        services.AddScoped<IMoloniSetsService, MoloniSetsService>();
        services.AddScoped<IMoloniTaxService, MoloniTaxService>();
        services.AddScoped<IMoloniInvoiceReceiptService, MoloniInvoiceReceiptService>();
        services.AddScoped<IMoloniSettingsProvider, MoloniSettingsProvider>();
        services.AddScoped<IMoloniWarehouseService, MoloniWarehouseService>();
        services.AddScoped<IMoloniMiscServices, MoloniMiscServices>();
        services.AddScoped<IMoloniCategoryService, MoloniCategoryService>();
        services.AddScoped<IMoloniStockService, MoloniStockService>();
        services.AddScoped<IMoloniReturnsService, MoloniReturnsService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        services.AddHttpClient("MoloniAPI", client =>
        {
            client.BaseAddress = new Uri("https://api.moloni.pt/v1/");
        });
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 3000;
}