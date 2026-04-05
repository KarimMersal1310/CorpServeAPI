using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities;

namespace CorpServe.Domain.Entities.VendorVerifyModule
{
    public class VendorVerify : BaseEntity<string>
    {
        public DateTime SubmittedAt { get; set; }
        public string OrganizationName { get; set; } = default!;
        public VerifyStatus Status { get; set; } = VerifyStatus.Pending;
        public DateTime? ReviewedAt { get; set; }
        public string? RejectReason { get; set; }
        #region RelationShips
        #region VendorVerify - VendorCertificate
        public ICollection<VendorCertificate> VendorCertificates { get; set; } = new List<VendorCertificate>();

        #endregion

        #region Vendor - VendorVerify
        public string VendorId { get; set; } = default!;
        public ApplicationUser Vendor { get; set; } = default!;
        #endregion
        #endregion
    }
}
