using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.EmailTemplates;
using CorpServe.Shared.DTOs.VendorVerify;
using CorpServe.Shared.CommonResult;
using CorpServe.Domain.Contracts;
using CorpServe.Services.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class AdminVendorService : IAdminVendorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminVendorService> _logger;

        public AdminVendorService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IEmailService emailService, INotificationService notificationService, ILogger<AdminVendorService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<VendorVerifyDTO>>> GetPendingVerificationsAsync()
        {
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var specification = new PendingVendorVerificationsSpecification();
            var pending = (await verifyRepo.GetAllAsync(specification)).ToList();

            var assignedCategoryIds = pending
                .SelectMany(p => p.Vendor?.VendorCategories?.Select(vc => vc.CategoryId) ?? Enumerable.Empty<string>())
                .Distinct()
                .ToList();

            var categoryNamesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (assignedCategoryIds.Count > 0)
            {
                var categoryRepo = _unitOfWork.GetRepository<Category, string>();
                var categories = await categoryRepo.GetAllAsync(new CategoriesByIdsSpecification(assignedCategoryIds));
                categoryNamesById = categories.ToDictionary(c => c.Id, c => c.Name, StringComparer.OrdinalIgnoreCase);
            }

            var dtos = new List<VendorVerifyDTO>();

            foreach (var p in pending)
            {
                var assignedCategories = p.Vendor?.VendorCategories
                    .Select(vc => categoryNamesById.GetValueOrDefault(vc.CategoryId))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                dtos.Add(new VendorVerifyDTO
                {
                    Id = p.Id,
                    VendorId = p.VendorId,
                    VendorName = p.Vendor?.FullName ?? "",
                    VendorEmail = p.Vendor?.Email ?? "",
                    AssignedCategories = assignedCategories,
                    OrganizationName = p.OrganizationName,
                    SubmittedAt = p.SubmittedAt,
                    Status = p.Status.ToString(),
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

            var approvedEmail = CorpServeEmailTemplateFactory.BuildVendorVerificationApproved(user.FullName ?? "Vendor");
            await _emailService.SendEmailAsync(user.Email!, approvedEmail.Subject, approvedEmail.Body);

            verify.Status = VerifyStatus.Approved;
            verify.ReviewedAt = DateTime.UtcNow;
            verify.RejectReason = null;

            verifyRepo.Update(verify);
            await _unitOfWork.SaveChangesAsync();

            var notificationResult = await _notificationService.SendNotificationAsync(
                verify.VendorId,
                NotificationTitles.VerificationApproved,
                "Your vendor verification was approved. You can now submit proposals.",
                NotificationTypes.Success,
                verify.Id,
                "VendorVerification");

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Verification approved but notification failed for vendor {VendorId}. Errors: {Errors}",
                    verify.VendorId,
                    string.Join(" | ", notificationResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }

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

            var rejectedEmail = CorpServeEmailTemplateFactory.BuildVendorVerificationRejected(user.FullName ?? "Vendor", rejectReason.Trim());
            await _emailService.SendEmailAsync(user.Email!, rejectedEmail.Subject, rejectedEmail.Body);

            verify.Status = VerifyStatus.Rejected;
            verify.ReviewedAt = DateTime.UtcNow;
            verify.RejectReason = rejectReason.Trim();

            verifyRepo.Update(verify);
            await _unitOfWork.SaveChangesAsync();

            var notificationResult = await _notificationService.SendNotificationAsync(
                verify.VendorId,
                NotificationTitles.VerificationRejected,
                $"Your vendor verification was rejected. Reason: {rejectReason.Trim()}",
                NotificationTypes.Warning,
                verify.Id,
                "VendorVerification");

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Verification rejected but notification failed for vendor {VendorId}. Errors: {Errors}",
                    verify.VendorId,
                    string.Join(" | ", notificationResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }

            return true;
        }

    }
}
