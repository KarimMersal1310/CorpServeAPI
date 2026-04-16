namespace CorpServe.Services.Abstraction
{
    public interface IVendorVerificationQuery
    {
        Task<bool> IsVendorApprovedAsync(string vendorId, CancellationToken cancellationToken = default);
    }
}
