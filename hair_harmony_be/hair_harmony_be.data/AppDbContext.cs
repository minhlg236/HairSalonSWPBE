using Microsoft.EntityFrameworkCore;
using hair_harmony_be.hair_harmony_be.repositoty.model;
using System.Collections.Generic;

namespace HairSalon.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
