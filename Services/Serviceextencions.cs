// ─── ServiceExtensions.cs ────────────────────────────────────────────────────
// ✅ CORREGIDO: separado en su propio archivo (C# no permite dos namespace
//    file-scoped en el mismo archivo). El using va aquí arriba, no en medio.
using EsotericStore.API.Services;

namespace EsotericStore.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        // services.AddScoped<ICategoryService, CategoryService>(); // agregar cuando lo implementes
        // services.AddScoped<IOrderService, OrderService>();       // ídem

        return services;
    }
}