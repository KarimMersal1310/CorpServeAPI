using CorpServe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.VendorVerifyModule
{
    public class VendorCertificate : BaseEntity<string>
    {
        public string FileUrl { get; set; } = default!;
        public string CertificateType { get; set; } = default!;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        #region RelationShips
        #region VendorVerify - VendorCertificate
        public string VendorVerifyId { get; set; } = default!;
        public VendorVerify VendorVerify { get; set; } = default!;  
        #endregion
        #endregion
    }
}
