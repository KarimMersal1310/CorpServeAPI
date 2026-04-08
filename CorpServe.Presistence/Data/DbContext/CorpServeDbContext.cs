using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.NotificationModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RatingModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Presistence.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Presistence.Data.DbContext
{
    public class CorpServeDbContext : IdentityDbContext<ApplicationUser>
    {
        public CorpServeDbContext(DbContextOptions<CorpServeDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<VendorCategory> VendorCategories { get; set; }
        public DbSet<VendorVerify> VendorVerifications { get; set; }
        public DbSet<VendorCertificate> VendorCertificates { get; set; }
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<SLAContract> SLAContracts { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.HasSequence<int>("RequestSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("RequestAttachmentSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("RequestProgressSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("VendorCertificateSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("AIEstimationSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("ProposalSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("SLAContractSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("NotificationSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("PaymentSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            builder.HasSequence<int>("RatingSequence")
                   .StartsAt(1)
                   .IncrementsBy(1);

            // Identity table renames
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");

            // Auto-apply all IEntityTypeConfiguration<T> in this assembly
            builder.ApplyConfigurationsFromAssembly(typeof(CorpServeDbContext).Assembly);
        }
    }
}
