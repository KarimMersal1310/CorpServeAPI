using CorpServe.Domain.Entities.IdentityModule;
using EventHub.Domain.Entities;

namespace CorpServe.Domain.Entities.VendorVerifyModule
{
    public class VendorVerify : BaseEntity<string>
    {
        public DateTime SubmittedAt { get; set; }
        public string OrganizationName { get; set; } = default!;

        public VerifyStatus Status { get; set; } = VerifyStatus.Pending;

        /// <summary>
        /// Date the admin responded (approved or rejected).
        /// NULL while the request is still Pending.
        /// Set to DateTime.UtcNow by the service layer when admin acts.
        /// </summary>
        public DateTime? ReviewedAt { get; set; }
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
