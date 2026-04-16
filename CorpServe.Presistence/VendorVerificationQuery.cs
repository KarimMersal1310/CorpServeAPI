using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Presistence.Data.DbContext;
using CorpServe.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Presistence
{
    public sealed class VendorVerificationQuery : IVendorVerificationQuery
    {
        private readonly CorpServeDbContext _dbContext;

        public VendorVerificationQuery(CorpServeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> IsVendorApprovedAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Task.FromResult(false);

            return _dbContext.VendorVerifications
                .AsNoTracking()
                .AnyAsync(v => v.VendorId == vendorId && v.Status == VerifyStatus.Approved, cancellationToken);
        }
    }
}
