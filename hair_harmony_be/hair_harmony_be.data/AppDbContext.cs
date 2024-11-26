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
        public DbSet<LevelAccount> LevelAccounts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Style> Styles { get; set; }
        public DbSet<Image> Images { get; set; }

        public DbSet<CommissionStaff> CommissionStaffs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ánh xạ tên bảng nếu bảng trong cơ sở dữ liệu có tên khác
            modelBuilder.Entity<LevelAccount>().ToTable("level_accounts"); // Ánh xạ tên bảng trong CSDL
            modelBuilder.Entity<Style>().ToTable("styles"); // Ánh xạ tên bảng trong CSDL


            // Ánh xạ mối quan hệ giữa User và Role qua cột `role_id`
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role) // Liên kết với thuộc tính điều hướng Role
                .WithMany()           // Role có thể có nhiều User
                .HasForeignKey("role_id"); // Chỉ rõ cột khoá ngoại trong bảng Users là "role_id"

            modelBuilder.Entity<User>()
                .HasOne(u => u.LevelAccount) // Liên kết với LevelAccount
                .WithMany()                   // LevelAccount có thể có nhiều User
                .HasForeignKey("level_accounts_id"); // Chỉ rõ cột khoá ngoại trong bảng Users là "level_accounts_id"

            // Configure Style entity
            modelBuilder.Entity<Style>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey("createdBy")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Style>()
                .HasOne(s => s.UpdatedBy)
                .WithMany()
                .HasForeignKey("updatedBy")
                .OnDelete(DeleteBehavior.Restrict);

            // Thiết lập mối quan hệ giữa Image và Style
            // Ánh xạ cột style_id trong cơ sở dữ liệu với thuộc tính StyleId trong lớp Image

            // Quan hệ giữa Image và Style: Một Style có nhiều Image
            modelBuilder.Entity<Image>()
                .HasOne(s => s.Style)
                .WithMany()
                .HasForeignKey("style_id")
                .OnDelete(DeleteBehavior.Restrict);


            // Configure Style entity
            modelBuilder.Entity<Style>()
                .HasOne(s => s.CreatedBy)  // Mối quan hệ với User (createBy)
                .WithMany()  // Một User có thể tạo nhiều Style
                .HasForeignKey("createdBy")  // Khóa ngoại trên createBy
                .OnDelete(DeleteBehavior.Restrict);  // Không cho phép xóa user khi có Style tạo bởi họ

            modelBuilder.Entity<Style>()
                .HasOne(s => s.UpdatedBy)  // Mối quan hệ với User (updateBy)
                .WithMany()  // Một User có thể cập nhật nhiều Style
                .HasForeignKey("updatedBy")  // Khóa ngoại trên updateBy
                .OnDelete(DeleteBehavior.Restrict);  // Không cho phép xóa user khi có Style được cập nhật bởi họ


        }
    }
}
