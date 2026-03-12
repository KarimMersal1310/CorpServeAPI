using System;
using System.Collections.Generic;

namespace CorpServe.Shared.DTOs.VendorVerify
{
    public class VendorVerifyDTO
    {
        public string Id { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string OrganizationName { get; set; } = default!;
        public DateTime SubmittedAt { get; set; }
        public int Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
        
        public IEnumerable<VendorCertificateDTO> Certificates { get; set; } = new List<VendorCertificateDTO>();
    }
}
