using Core.Concrete.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class PaymentContext : DbContext
    {
        public PaymentContext(DbContextOptions<PaymentContext> options) : base(options)
        {
        }
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentLog> PaymentLogs { get; set; } = null!;

    }

}
