using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Data.Interceptors;

namespace Payment;

public static class PaymentModule
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<PaymentDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    public static IApplicationBuilder UsePaymentModule(this IApplicationBuilder app)
    {
        app.UseMigration<PaymentDbContext>();

        return app;
    }
}
