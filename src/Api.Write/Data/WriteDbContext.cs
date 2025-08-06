using Microsoft.EntityFrameworkCore;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Data
{
    public class WriteDbContext : DbContext
    {
        // --- DbSets ---
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<Credential> Credentials { get; set; } = null!;
        public DbSet<Developer> Developers { get; set; } = null!;
        public DbSet<App> Apps { get; set; } = null!;
        public DbSet<AppVersion> AppVersions { get; set; } = null!;
        public DbSet<AppFile> AppFiles { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<AbuseReport> AbuseReports { get; set; } = null!;
        public DbSet<Purchase> Purchases { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<Payout> Payouts { get; set; } = null!;
        public DbSet<ModerationAction> ModerationActions { get; set; } = null!;
        public DbSet<SystemLog> SystemLogs { get; set; } = null!;

        public WriteDbContext(DbContextOptions<WriteDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === Entity Configurations ===

            // --- User ---
            modelBuilder.Entity<User>(e =>
            {
                e.Property(u => u.IsEmailVerified).HasDefaultValue(false);

                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.Username).IsUnique();

                // 1-to-1 with Credential
                e.HasOne(u => u.Credential)
                 .WithOne(c => c.User)
                 .HasForeignKey<Credential>(c => c.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                // 1-to-1 with Developer
                e.HasOne(u => u.Developer)
                 .WithOne(d => d.User)
                 .HasForeignKey<Developer>(d => d.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasIndex(r => r.Name).IsUnique();
                entity.HasData(
                    new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "User" },
                    new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Developer" },
                    new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Admin" }
                );
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            });

            modelBuilder.Entity<App>(entity =>
            {
                entity.ToTable("Apps");
                entity.Property(a => a.Price).HasColumnType("decimal(18, 2)");

                // Configure the enum-to-string conversion for the Status property
                entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(50);

                // --- NEW, CORRECT SYNTAX FOR CHECK CONSTRAINT ---
                // Apply the constraint to the table itself
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Apps_Status", "[Status] IN ('Draft', 'Pending', 'Published', 'Rejected', 'Banned')"));
            });

            modelBuilder.Entity<AppVersion>(entity =>
            {
                entity.ToTable("AppVersions");
                entity.HasIndex(v => new { v.AppId, v.VersionNumber }).IsUnique();
            });

            modelBuilder.Entity<AppFile>(entity =>
            {
                entity.ToTable("AppFiles");
                entity.HasIndex(f => f.Checksum).IsUnique();
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("Reviews");
                entity.HasIndex(r => new { r.AppId, r.UserId }).IsUnique();
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Reviews_Rating", "[Rating] >= 1 AND [Rating] <= 5"));

                entity.HasOne(r => r.App)
                      .WithMany(a => a.Reviews)
                      .HasForeignKey(r => r.AppId)
                      .OnDelete(DeleteBehavior.Cascade); // Cascade delete reviews when an App is deleted

                entity.HasOne(r => r.User)
                      .WithMany(u => u.Reviews)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // Set UserId to null if User is deleted
            });

            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.ToTable("Purchases");
                entity.HasIndex(p => new { p.UserId, p.AppId }).IsUnique();
                entity.Property(p => p.Price).HasColumnType("decimal(18, 2)");
                entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Purchases_Status", "[Status] IN ('Pending', 'Completed', 'Refunded')"));

                // If a User is deleted, DO NOT automatically delete their purchases.
                // This preserves the financial audit trail.
                entity.HasOne(p => p.User)
                      .WithMany(u => u.Purchases)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // CHANGE FROM CASCADE (default) TO RESTRICT

                // Also, if an App is deleted, we should preserve the purchase history.
                entity.HasOne(p => p.App)
                      .WithMany() // App might not have a direct navigation back to Purchases
                      .HasForeignKey(p => p.AppId)
                      .OnDelete(DeleteBehavior.Restrict); // CHANGE FROM CASCADE TO RESTRICT
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasIndex(t => new { t.PaymentProvider, t.PaymentId }).IsUnique();
                entity.Property(t => t.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(50);
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Transactions_Status", "[Status] IN ('Pending', 'Completed', 'Failed')"));
            });

            modelBuilder.Entity<Payout>(entity =>
            {
                entity.ToTable("Payouts");
                entity.Property(p => p.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Payouts_Status", "[Status] IN ('Scheduled', 'Processing', 'Processed', 'Cancelled', 'Failed')"));
            });

            modelBuilder.Entity<AbuseReport>(entity =>
            {
                entity.ToTable("AbuseReports");
                entity.Property(ar => ar.Status).HasConversion<string>().HasMaxLength(50);
                entity.ToTable(tb => tb.HasCheckConstraint("CK_AbuseReports_Status", "[Status] IN ('New', 'UnderReview', 'Resolved')"));
                entity.ToTable(tb => tb.HasCheckConstraint("CK_AbuseReports_HasTarget", "[ReviewId] IS NOT NULL OR [AppId] IS NOT NULL OR [UserId] IS NOT NULL"));

                entity.HasOne(ar => ar.Reporter) // Each AbuseReport has one Reporter...
                    .WithMany(u => u.FiledAbuseReports) // ...and a User can file many reports.
                    .HasForeignKey(ar => ar.ReporterId) // The link is via the ReporterId foreign key.
                    .OnDelete(DeleteBehavior.Restrict); // Prevent a user from being deleted if they have filed reports.

                // Configure the "Reported User" relationship
                entity.HasOne(ar => ar.ReportedUser) // Each AbuseReport has one (or zero) ReportedUser...
                    .WithMany(u => u.AbuseReportsAgainstUser) // ...and a User can have many reports against them.
                    .HasForeignKey(ar => ar.UserId) // The link is via the UserId foreign key.
                    .IsRequired(false) // This foreign key is optional/nullable.
                    .OnDelete(DeleteBehavior.Restrict); // Prevent a user from being deleted if they are the subject of a report.
            });

            modelBuilder.Entity<ModerationAction>(entity =>
            {
                entity.ToTable("ModerationActions");
                entity.Property(ma => ma.Status).HasConversion<string>().HasMaxLength(50);
                entity.Property(ma => ma.TargetType).HasConversion<string>().HasMaxLength(50);
                entity.ToTable(tb => tb.HasCheckConstraint("CK_ModerationActions_Status", "[Status] IN ('Pending', 'Completed', 'Reversed')"));
            });

            // === Relationship Configurations (It's often clearer to group these) ===

            // --- One-to-One Relationships ---
            modelBuilder.Entity<User>()
                .HasOne(u => u.Credential).WithOne(c => c.User).HasForeignKey<Credential>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
                .HasOne(u => u.Developer).WithOne(d => d.User).HasForeignKey<Developer>(d => d.UserId).OnDelete(DeleteBehavior.Cascade);

            // --- One-to-Many Relationships ---
            modelBuilder.Entity<App>()
                .HasMany(a => a.Versions).WithOne(v => v.App).HasForeignKey(v => v.AppId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<App>()
                .HasMany(a => a.Reviews).WithOne(r => r.App).HasForeignKey(r => r.AppId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Purchase>()
                .HasMany(p => p.Transactions).WithOne(t => t.Purchase).HasForeignKey(t => t.PurchaseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SystemLog>()
                .HasOne(sl => sl.User).WithMany().HasForeignKey(sl => sl.UserId).OnDelete(DeleteBehavior.SetNull);

            // --- Many-to-Many 
            // --- ADD THIS BLOCK INSTEAD ---
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");

                // Define the composite primary key
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                // Define the relationship to User
                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.Roles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade); // Or Restrict if you prefer

                // Define the relationship to Role
                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UsersOfRole)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade); // Or Restrict
            });
        }
    }
}
