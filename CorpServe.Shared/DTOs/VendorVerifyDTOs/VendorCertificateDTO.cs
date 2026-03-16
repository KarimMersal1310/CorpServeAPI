using System;

namespace CorpServe.Shared.DTOs.VendorVerify
{
    public class VendorCertificateDTO
    {
        public string Id { get; set; } = default!;
        public string FileUrl { get; set; } = default!;
        public string CertificateType { get; set; } = default!;
        public DateTime UploadedAt { get; set; }
    }
}
