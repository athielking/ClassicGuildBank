using ClassicGuildBankData.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicGuildBankData.Data
{
    public class ClassicGuildBankDbContext : IdentityDbContext<ClassicGuildBankUser>
    {
        #region Data Members

        private IConfiguration _configuration;

        #endregion

        #region Properties

        public DbSet<Guild> Guilds { get; set; }
        public DbSet<GuildMember> GuildMembers { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Bag> Bags { get; set; }
        public DbSet<BagSlot> BagSlots { get; set; }
        public DbSet<Item> Items { get; set; }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }

        public DbSet<ItemRequest> ItemRequests { get; set; }
        public DbSet<ItemRequestDetail> ItemRequestDetails { get; set; }

        #endregion

        #region Constructor

        public ClassicGuildBankDbContext(IConfiguration config)
        {
            _configuration = config;
        }

        public ClassicGuildBankDbContext(DbContextOptions<ClassicGuildBankDbContext> options, IConfiguration config) : base(options)
        {
            _configuration = config;
        }

        #endregion

        #region Overrides

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("ClassicGuildBankDb"));

            optionsBuilder.EnableSensitiveDataLogging(true);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Item");
                entity.HasKey(i => i.Id);
            });

            modelBuilder.Entity<BagSlot>(entity =>
            {
                entity.ToTable("BagSlot");
                entity.HasKey(bs => new { bs.BagId, bs.SlotId });
                entity.HasOne<Item>(bs => bs.Item)
                    .WithMany(i => i.Slots)
                    .HasForeignKey(bs => bs.ItemId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Bag>(bs => bs.Bag)
                    .WithMany(b => b.BagSlots)
                    .HasForeignKey(bs => bs.BagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Bag>(entity =>
            {
                entity.ToTable("Bag");
                entity.HasKey(b => b.Id);
                entity.HasOne<Item>(b => b.BagItem)
                    .WithMany(i => i.Bags)
                    .HasForeignKey(b => b.ItemId)
                    .IsRequired(false);

                entity.HasOne<Character>(b => b.Character)
                    .WithMany(c => c.Bags)
                    .HasForeignKey(b => b.CharacterId)
                    .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<Character>(entity =>
            {
                entity.ToTable("Character");
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.Guild)
                    .WithMany(g => g.Characters)
                    .HasForeignKey(c => c.GuildId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Guild>(entity =>
            {
                entity.ToTable("Guild");
                entity.HasKey(g => g.Id);
                entity.Property(g => g.PublicLinkEnabled).HasDefaultValue(false);
            });

            modelBuilder.Entity<GuildMember>(entity =>
            {
                entity.ToTable("GuildRole");
                entity.HasKey(g => new { g.GuildId, g.UserId });

                entity.HasOne(c => c.Guild)
                    .WithMany(g => g.GuildMembers)
                    .HasForeignKey(c => c.GuildId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transaction");
                entity.HasKey(t => t.Id);
                
                entity.HasOne(t => t.Guild)
                    .WithMany(g => g.Transactions)
                    .HasForeignKey(t => t.GuildId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TransactionDetail>(entity =>
            {
                entity.ToTable("TransactionDetail");
                entity.HasKey(d => d.Id);

                entity.HasOne(d => d.Transaction)
                    .WithMany(i => i.TransactionDetails)
                    .HasForeignKey(d => d.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Item)
                    .WithMany(i => i.TransactionDetails)
                    .HasForeignKey(d => d.ItemId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ItemRequest>(entity =>
            {
                entity.ToTable("ItemRequest");
                entity.HasKey(t => t.Id);

                entity.HasOne(t => t.Guild)
                    .WithMany(g => g.ItemRequests)
                    .HasForeignKey(i => i.GuildId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemRequestDetail>(entity =>
            {
                entity.ToTable("ItemRequestDetail");
                entity.HasKey(d => d.Id);
                entity.HasOne(d => d.ItemRequest)
                    .WithMany(i => i.Details)
                    .HasForeignKey(d => d.ItemRequestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Item)
                    .WithMany(i => i.ItemRequestDetails)
                    .HasForeignKey(d => d.ItemId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        } 
        
        #endregion
    }
}
