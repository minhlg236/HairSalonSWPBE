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
        public DbSet<Style> Styles { get; set; }
        public DbSet<Image> Images { get; set; }

        public DbSet<CommissionStaff> CommissionStaffs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           

            // Ánh xạ mối quan hệ giữa User và Role qua cột `role_id`
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role) // Liên kết với thuộc tính điều hướng Role
                .WithMany()           // Role có thể có nhiều User
                .HasForeignKey("role_id"); // Chỉ rõ cột khoá ngoại trong bảng Users là "role_id"

           

        }
    }
}
