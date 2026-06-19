using InventoryCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryData.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<IdempotencyRequest> IdempotencyRequests { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(x => x.Product_Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Price)
                    .HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(x => x.ShippAddress)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<int>();

                entity.Property(x => x.RowVersion)
                    .IsRowVersion();
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(x => x.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.Order)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.OrderId);

                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId);
            });

            modelBuilder.Entity<IdempotencyRequest>(entity =>
            {
                entity.Property(x => x.Key)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.RequestHash)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => x.Key)
                    .IsUnique();
            });
        }
    }
}