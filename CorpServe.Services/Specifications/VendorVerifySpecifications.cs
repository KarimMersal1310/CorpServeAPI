using CorpServe.Domain.Entities.VendorVerifyModule;

namespace EventHub.Services.Specifications
{
    public sealed class VendorVerifyActiveRequestByVendorSpecification : BaseSpecificactions<VendorVerify, string>
    {
        public VendorVerifyActiveRequestByVendorSpecification(string vendorId)
            : base(v => v.VendorId == vendorId && (v.Status == VerifyStatus.Pending || v.Status == VerifyStatus.Approved))
        {
        }
    }

    public sealed class VendorVerifyLatestByVendorSpecification : BaseSpecificactions<VendorVerify, string>
    {
        public VendorVerifyLatestByVendorSpecification(string vendorId)
            : base(v => v.VendorId == vendorId)
        {
            AddInclude(v => v.VendorCertificates);
            AddOrderByDescending(v => v.SubmittedAt);
        }
    }

    public sealed class PendingVendorVerificationsSpecification : BaseSpecificactions<VendorVerify, string>
    {
        public PendingVendorVerificationsSpecification()
            : base(v => v.Status == VerifyStatus.Pending)
        {
            AddInclude(v => v.VendorCertificates);
            AddOrderByDescending(v => v.SubmittedAt);
        }
    }
}
