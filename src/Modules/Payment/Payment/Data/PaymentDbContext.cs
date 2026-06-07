namespace Payment.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("payment");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());        
        base.OnModelCreating(builder);
    }
}
