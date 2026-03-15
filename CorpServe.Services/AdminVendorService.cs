using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.VendorVerify;
using CorpServe.Shared.CommonResult;
using EventHub.Domain.Contracts;
using EventHub.Services.Specifications;
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
        private readonly IEmailService _emailService;

        public AdminVendorService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<Result<IEnumerable<VendorVerifyDTO>>> GetPendingVerificationsAsync()
        {
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var specification = new PendingVendorVerificationsSpecification();
            var pending = (await verifyRepo.GetAllAsync(specification)).ToList();
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
                    ReviewedAt = p.ReviewedAt,
                    RejectReason = p.RejectReason,
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

            var user = await _userManager.FindByIdAsync(verify.VendorId);
            if (user == null) return Error.NotFound("Vendor.NotFound", "Vendor not found.");

            var approvedEmail = GetVendorVerificationEmail(user.FullName ?? "Vendor", true);
            await _emailService.SendEmailAsync(user.Email!, approvedEmail.Subject, approvedEmail.Body);

            verify.Status = VerifyStatus.Approved;
            verify.ReviewedAt = DateTime.UtcNow;
            verify.RejectReason = null;

            verifyRepo.Update(verify);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<Result<bool>> RejectVerificationAsync(string vendorVerifyId, string adminId, string rejectReason)
        {
            if (string.IsNullOrWhiteSpace(rejectReason))
                return Error.Validation("VendorVerify.RejectReasonRequired", "Reject reason is required.");

            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var verify = await verifyRepo.GetByIdAsync(vendorVerifyId);
            if (verify == null) return Error.NotFound("VendorVerify.NotFound", "Verification request not found.");

            var user = await _userManager.FindByIdAsync(verify.VendorId);
            if (user == null) return Error.NotFound("Vendor.NotFound", "Vendor not found.");

            var rejectedEmail = GetVendorVerificationEmail(user.FullName ?? "Vendor", false, rejectReason.Trim());
            await _emailService.SendEmailAsync(user.Email!, rejectedEmail.Subject, rejectedEmail.Body);

            verify.Status = VerifyStatus.Rejected;
            verify.ReviewedAt = DateTime.UtcNow;
            verify.RejectReason = rejectReason.Trim();

            verifyRepo.Update(verify);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private (string Subject, string Body) GetVendorVerificationEmail(string vendorName, bool isApproved, string? rejectReason = null)
        {
            if (isApproved)
            {
                string subject = "Vendor Verification Approved 🎉";

                string body = $@"
                            <html>
                            <body style='font-family:Arial; background-color:#f5f5f5; padding:20px;'>

                            <div style='max-width:600px; margin:auto; background:#ffffff; padding:30px; border-radius:8px;'>

                            <h2 style='color:#28a745;'>Vendor Verification Approved 🎉</h2>

                            <p>Dear <strong>{vendorName}</strong>,</p>

                            <p>
                            We are pleased to inform you that your vendor verification request has been 
                            <strong>successfully approved</strong>.
                            </p>

                            <p>Your account is now marked as a <strong>Verified Vendor</strong> on our platform. You can now:</p>

                            <ul>
                                <li>Submit proposals to client requests</li>
                                <li>Collaborate with clients</li>
                                <li>Access vendor features on the platform</li>
                            </ul>

                            <p>If you need any assistance, please contact our support team.</p>

                            <br/>

                            <p>Best regards,<br/>
                            <strong>CorpServe Team</strong></p>

                            </div>

                            </body>
                            </html>";

                return (subject, body);
            }
            else
            {
                string subject = "Vendor Verification Request Update";

                string body = $@"
                        <html>
                        <body style='font-family:Arial; background-color:#f5f5f5; padding:20px;'>

                        <div style='max-width:600px; margin:auto; background:#ffffff; padding:30px; border-radius:8px;'>

                        <h2 style='color:#dc3545;'>Vendor Verification Request Update</h2>

                        <p>Dear <strong>{vendorName}</strong>,</p>

                        <p>Thank you for submitting your vendor verification request.</p>

                        <p>
                        After reviewing your application, we regret to inform you that 
                        <strong>your verification request has been rejected</strong>.
                        </p>

                        <p><strong>Reason for Rejection:</strong></p>

                        <div style='background:#fff3f3; border-left:4px solid #dc3545; padding:10px; margin:10px 0;'>
                        {rejectReason}
                        </div>

                        <p>
                        Please review the reason above and update the required information or documents
                        before submitting another verification request.
                        </p>

                        <p>If you need clarification or assistance, please contact our support team.</p>

                        <br/>

                        <p>Best regards,<br/>
                        <strong>CorpServe Team</strong></p>

                        </div>

                        </body>
                        </html>";

                return (subject, body);
            }
        }
    }
}
