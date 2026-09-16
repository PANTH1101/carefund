using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NGODonationSystem.Models;

namespace NGODonationSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NGO> NGOs { get; set; }

        public DbSet<NGODocument> NGODocuments { get; set; }

        public DbSet<Campaign> Campaigns { get; set; }

        public DbSet<Donation> Donations { get; set; }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Enforce one user = one role by adding unique index on UserId in AspNetUserRoles
            builder.Entity<IdentityUserRole<string>>()
                .HasIndex(ur => ur.UserId)
                .IsUnique();

            builder.Entity<NGO>()
                .HasOne(n => n.User)
                .WithOne()
                .HasForeignKey<NGO>(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NGODocument>()
                .HasOne(d => d.NGO)
                .WithMany(n => n.Documents)
                .HasForeignKey(d => d.NGOId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Campaign>()
                .HasOne(c => c.NGO)
                .WithMany(n => n.Campaigns)
                .HasForeignKey(c => c.NGOId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Donation>()
                .HasOne(d => d.Donor)
                .WithMany()
                .HasForeignKey(d => d.DonorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Donation>()
                .HasOne(d => d.NGO)
                .WithMany()
                .HasForeignKey(d => d.NGOId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Donation>()
                .HasOne(d => d.Campaign)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Donation)
                .WithOne(d => d.Payment)
                .HasForeignKey<Payment>(p => p.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NGO>()
                .Property(n => n.VerificationStatus)
                .HasMaxLength(20);

            builder.Entity<Donation>()
                .Property(d => d.Status)
                .HasMaxLength(30);

            builder.Entity<Payment>()
                .Property(p => p.Status)
                .HasMaxLength(30);

            builder.Entity<Campaign>()
                .Property(c => c.TargetAmount)
                .HasPrecision(18, 2);

            builder.Entity<Donation>()
                .Property(d => d.Amount)
                .HasPrecision(18, 2);
        }
    }
}