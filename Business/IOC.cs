using Business.Managements;
using Core.Abstract.IManagements;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business
{
    public static class IOC
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PaymentContext>(options => options.UseSqlite(configuration.GetConnectionString("data")));

            services.AddScoped<IPaymentManager, PaymentManager>();
            return services;
        }
    }

}
