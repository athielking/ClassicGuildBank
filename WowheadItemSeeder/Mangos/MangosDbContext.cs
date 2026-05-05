using System;
using Microsoft.EntityFrameworkCore;

namespace WowheadItemSeeder.Mangos
{
    public class MangosDbContext : DbContext
    {
        public DbSet<ItemTemplate> ItemTemplates { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=localhost;database=classicmangos;user=root;password=P@ssw0rd!", new MySqlServerVersion(new Version(8, 0, 0)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ItemTemplate>(entity => 
            {
                entity.ToTable("item_template");
                entity.HasKey(i => i.entry);
                entity.Property(i => i._class).HasColumnName("class");
            });
        }
    }
}
