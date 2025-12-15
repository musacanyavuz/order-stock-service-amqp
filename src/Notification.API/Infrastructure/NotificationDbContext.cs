using Microsoft.EntityFrameworkCore;
using Notification.API.Models;

namespace Notification.API.Infrastructure
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<NotificationRecord> NotificationRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NotificationRecord>().HasKey(x => x.Id);
        }
    }
}
