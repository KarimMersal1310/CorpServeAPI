using System;

namespace CorpServe.Shared.DTOs.VendorVerify
{
    public class VendorCertificateDTO
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = default!;
        public string CertificateType { get; set; } = default!;
        public DateTime UploadedAt { get; set; }
    }
}
