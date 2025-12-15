using MassTransit;
using Microsoft.EntityFrameworkCore;
using Stock.API.Models;
using System;

namespace Stock.API.Infrastructure
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
        {
        }

        public DbSet<Models.Stock> Stocks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<Models.Stock>(entity =>
            {
                entity.ToTable("Stocks");
                entity.HasKey(e => e.Id);
                
                // Use PostgreSQL system column 'xmin' for concurrency
                entity.Property<uint>("xmin").IsRowVersion();
            });
            
            // Seed Data
            modelBuilder.Entity<Models.Stock>().HasData(
                new Models.Stock { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), ProductId = Guid.Parse("d8d47424-0c5a-4e2b-b5d1-93335555d444"), Count = 100 },
                new Models.Stock { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), ProductId = Guid.Parse("f9e58735-1d6b-5f3c-c6e2-04446666e555"), Count = 200 }
            );
        }
    }
}
