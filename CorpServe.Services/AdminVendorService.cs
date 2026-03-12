using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.VendorVerify;
using E_Commerce.Shared.CommonResult;
using EventHub.Domain.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.AspNetCore.Identity;

namespace CorpServe.Services
{
    public class AdminVendorService : IAdminVendorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminVendorService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Result<IEnumerable<VendorVerifyDTO>>> GetPendingVerificationsAsync()
        {
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var allVerifications = await verifyRepo.GetAllAsync();
            
            var pending = allVerifications.Where(v => v.Status == VerifyStatus.Pending).ToList();
            var dtos = new List<VendorVerifyDTO>();

            foreach (var p in pending)
            {
                var user = await _userManager.FindByIdAsync(p.VendorId);
                dtos.Add(new VendorVerifyDTO
                {
                    Id = p.Id,
                    VendorId = p.VendorId,
                    VendorName = user?.FullName ?? "",
                    OrganizationName = p.OrganizationName,
                    SubmittedAt = p.SubmittedAt,
                    Status = (int)p.Status,
                    Certificates = p.VendorCertificates?.Select(c => new VendorCertificateDTO
                    {
                        Id = c.Id,
                        FileUrl = c.FileUrl,
                        CertificateType = c.CertificateType,
                        UploadedAt = c.UploadedAt
                    }).ToList() ?? new List<VendorCertificateDTO>()
                });
            }

            return dtos;
        }

        public async Task<Result<bool>> ApproveVerificationAsync(string vendorVerifyId, string adminId)
        {
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var verify = await verifyRepo.GetByIdAsync(vendorVerifyId);
            if (verify == null) return Error.NotFound("VendorVerify.NotFound", "Verification request not found.");

            verify.Status = VerifyStatus.Approved;
            verify.ReviewedAt = DateTime.UtcNow;

            verifyRepo.Update(verify);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<Result<bool>> RejectVerificationAsync(string vendorVerifyId, string adminId)
        {
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var verify = await verifyRepo.GetByIdAsync(vendorVerifyId);
            if (verify == null) return Error.NotFound("VendorVerify.NotFound", "Verification request not found.");

            verify.Status = VerifyStatus.Rejected;
            verify.ReviewedAt = DateTime.UtcNow;

            verifyRepo.Update(verify);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
