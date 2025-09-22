using Microsoft.EntityFrameworkCore;
using TestCurrConverter.Data.Models;

namespace TestCurrConverter.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)             {
        }

        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<CurrencyPair> CurrencyPairs { get; set; }
        public DbSet<ApiUsage> ApiUsages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.HasIndex(e => new { e.BaseCurrency, e.TargetCurrency, e.Date })
                      .IsUnique();

                entity.Property(e => e.Rate)
                      .HasPrecision(18, 6);
            });

            modelBuilder.Entity<CurrencyPair>(entity =>
            {
                entity.HasIndex(e => new { e.BaseCurrency, e.TargetCurrency })
                      .IsUnique();
            });

            modelBuilder.Entity<ApiUsage>(entity =>
            {
                entity.HasIndex(e => e.ApiKey);
            });

            // Seed data
            modelBuilder.Entity<CurrencyPair>().HasData(
                new CurrencyPair { Id = 1, BaseCurrency = "USD", TargetCurrency = "GBP" },
                new CurrencyPair { Id = 2, BaseCurrency = "USD", TargetCurrency = "EUR" },
                new CurrencyPair { Id = 3, BaseCurrency = "GBP", TargetCurrency = "EUR" },
                new CurrencyPair { Id = 4, BaseCurrency = "USD", TargetCurrency = "JPY" }
            );
        }
    }
}
