namespace WebAuction.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ModelBaze : DbContext
    {
        public ModelBaze()
            : base("name=ModelBazeAuction")
        {
        }

        public virtual DbSet<Auction> Auctions { get; set; }
        public virtual DbSet<Bid> Bids { get; set; }
        public virtual DbSet<SystemParameter> SystemParameters { get; set; }
        public virtual DbSet<TokenOder> TokenOders { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Auction>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Auction>()
                .Property(e => e.StartPrice)
                .HasPrecision(18, 0);

            modelBuilder.Entity<Auction>()
                .Property(e => e.CurrentPrice)
                .HasPrecision(18, 0);

            modelBuilder.Entity<Auction>()
                .Property(e => e.Currency)
                .IsUnicode(false);

            modelBuilder.Entity<Auction>()
                .Property(e => e.State)
                .IsUnicode(false);

            modelBuilder.Entity<Auction>()
                .HasMany(e => e.Bids)
                .WithRequired(e => e.Auction1)
                .HasForeignKey(e => e.Auction)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Bid>()
                .Property(e => e.Currency)
                .IsUnicode(false);

            modelBuilder.Entity<SystemParameter>()
                .Property(e => e.Currency)
                .IsUnicode(false);

            modelBuilder.Entity<SystemParameter>()
                .Property(e => e.TokensPrice)
                .HasPrecision(18, 0);

            modelBuilder.Entity<TokenOder>()
                .Property(e => e.Price)
                .HasPrecision(18, 0);

            modelBuilder.Entity<TokenOder>()
                .Property(e => e.Currency)
                .IsUnicode(false);

            modelBuilder.Entity<TokenOder>()
                .Property(e => e.Status)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.FirstName)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.LastName)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Email)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Address)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Password)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Bids)
                .WithRequired(e => e.User)
                .HasForeignKey(e => e.Bidder)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.TokenOders)
                .WithRequired(e => e.User)
                .HasForeignKey(e => e.Buyer)
                .WillCascadeOnDelete(false);
        }
    }
}
