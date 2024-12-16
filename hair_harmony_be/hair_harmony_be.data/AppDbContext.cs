using Microsoft.EntityFrameworkCore;
using hair_harmony_be.hair_harmony_be.repositoty.model;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HairSalon.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<CategoryService> CategoryServices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           

          
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role) 
                .WithMany()         
                .HasForeignKey("role_id"); 

          
            modelBuilder.Entity<Image>()
                .HasOne(u => u.ServiceEntity)
                .WithMany()           
                .HasForeignKey("serviceId");

            modelBuilder.Entity<Service>()
               .HasOne(u => u.UpdatedBy)
               .WithMany()
               .HasForeignKey("updatedBy");

            modelBuilder.Entity<Service>()
               .HasOne(u => u.CreatedBy)
               .WithMany()
               .HasForeignKey("createdBy");

            modelBuilder.Entity<Service>()
             .HasOne(u => u.CategoryService)
             .WithMany()
             .HasForeignKey("categoryServiceId");

            // booking
            modelBuilder.Entity<Booking>()
            .HasOne(b => b.Service)
            .WithMany()
           .HasForeignKey("serviceId");

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.CreatedBy)
                .WithMany()
                .HasForeignKey("createdBy");

            // payment
            modelBuilder.Entity<PaymentTransaction>()
           .HasOne(b => b.Booking)
           .WithMany()
          .HasForeignKey("bookingId");

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(b => b.CreatedBy)
                .WithMany()
                .HasForeignKey("createdBy");

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(b => b.UpdatedBy)
                .WithMany()
                .HasForeignKey("updatedBy");

            modelBuilder.Entity<PaymentTransaction>()
               .HasOne(b => b.Stylist)
               .WithMany()
               .HasForeignKey("stylistId");

            // category
    

            modelBuilder.Entity<CategoryService>()
                .HasOne(b => b.CreatedBy)
                .WithMany()
                .HasForeignKey("createdBy");

            modelBuilder.Entity<CategoryService>()
                .HasOne(b => b.UpdatedBy)
                .WithMany()
                .HasForeignKey("updatedBy");

           
        }
    }
}
