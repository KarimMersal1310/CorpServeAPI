using System;
using System.Collections.Generic;

namespace CorpServe.Shared.DTOs.VendorVerify
{
    public class VendorVerifyDTO
    {
        public string Id { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string VendorEmail { get; set; } = default!;
        public IEnumerable<string> AssignedCategories { get; set; } = new List<string>();
        public string OrganizationName { get; set; } = default!;
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = default!;
        public DateTime? ReviewedAt { get; set; }
        public string? RejectReason { get; set; }
        
        public IEnumerable<VendorCertificateDTO> Certificates { get; set; } = new List<VendorCertificateDTO>();
    }
}
