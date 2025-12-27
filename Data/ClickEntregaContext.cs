using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClickEntrega.Models;

namespace ClickEntrega.Data
{
    public class ClickEntregaContext : DbContext
    {
        public ClickEntregaContext (DbContextOptions<ClickEntregaContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Client { get; set; } = default!;
        public DbSet<Product> Product { get; set; } = default!;
        public DbSet<Category> Category { get; set; } = default!;
        public DbSet<Order> Order { get; set; } = default!;
        public DbSet<OrderItem> OrderItem { get; set; } = default!;
        public DbSet<Courier> Courier { get; set; } = default!;
        public DbSet<Delivery> Delivery { get; set; } = default!;
        public DbSet<Payment> Payment { get; set; } = default!;
        public DbSet<Review> Review { get; set; } = default!;
        public DbSet<Notification> Notification { get; set; } = default!;
        public DbSet<Company> Company { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Client>().ToTable("client");
            modelBuilder.Entity<Product>().ToTable("product");
            modelBuilder.Entity<Category>().ToTable("category");
            modelBuilder.Entity<Order>().ToTable("order");
            modelBuilder.Entity<OrderItem>().ToTable("order_item");
            modelBuilder.Entity<Courier>().ToTable("courier");
            modelBuilder.Entity<Delivery>().ToTable("delivery");
            modelBuilder.Entity<Payment>().ToTable("payment");
            modelBuilder.Entity<Review>().ToTable("review");
            modelBuilder.Entity<Notification>().ToTable("notification");
            modelBuilder.Entity<Company>().ToTable("company");

            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Company)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
