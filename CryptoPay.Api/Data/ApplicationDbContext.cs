using Microsoft.EntityFrameworkCore;
using CryptoPay.Api.Models;

namespace CryptoPay.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<PaymentIntent> PaymentIntents { get; set; }
    public DbSet<WalletAddress> WalletAddresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ApiKey).IsUnique();
        });

        modelBuilder.Entity<PaymentIntent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderRef);
            entity.HasIndex(e => e.PayAddress);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.PaymentIntents)
                .HasForeignKey(e => e.MerchantId);
        });

        modelBuilder.Entity<WalletAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Network, e.IsAssigned });
            entity.HasOne(e => e.PaymentIntent)
                .WithOne()
                .HasForeignKey<WalletAddress>(e => e.PaymentIntentId)
                .IsRequired(false);
        });
    }
}
