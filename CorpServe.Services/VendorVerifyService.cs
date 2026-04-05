using AutoMapper;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.VendorVerify;
using CorpServe.Shared.CommonResult;
using CorpServe.Domain.Contracts;
using CorpServe.Services.Specifications;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class VendorVerifyService : IVendorVerifyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly ILogger<VendorVerifyService> _logger;

        public VendorVerifyService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            INotificationService notificationService,
            ILogger<VendorVerifyService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _mapper = mapper;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<VendorVerifyDTO>> SubmitVerificationAsync(string vendorId, VendorVerifyRequestDTO request)
        {
            // 1. Validation limits (Max 3 Documents)
            if (request.Documents == null || request.Documents.Length == 0)
                return Error.Validation("VendorVerify.DocumentsRequired", "At least one document is required.");

            if (request.Documents.Length > 3)
                return Error.Validation("VendorVerify.MaxDocuments", "You can upload a maximum of 3 documents.");

            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();

            var activeRequestSpecification = new VendorVerifyActiveRequestByVendorSpecification(vendorId);
            var hasActiveRequest = await verifyRepo.CountAsync(activeRequestSpecification);
            if (hasActiveRequest > 0)
            {
                return Error.Conflict("VendorVerify.AlreadyExists", "You already have a pending or approved verification request.");
            }

            // 2. Create the entity
            var vendorVerify = new VendorVerify
            {
                VendorId = vendorId,
                OrganizationName = request.OrganizationName,
                SubmittedAt = DateTime.UtcNow,
                Status = VerifyStatus.Pending
            };

            // 4. Handle files
            foreach (var document in request.Documents)
            {
                if (document.Length > 0)
                {
                    var fileUrl = await _fileStorageService.UploadAsync(document, "uploads/vendor-certificates");

                    vendorVerify.VendorCertificates.Add(new VendorCertificate
                    {
                        FileUrl = fileUrl,
                        CertificateType = document.ContentType,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            // 5. Save entity
            await verifyRepo.AddAsync(vendorVerify);
            await _unitOfWork.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(vendorId);
            var vendorVerifyDto = _mapper.Map<VendorVerifyDTO>(vendorVerify);
            vendorVerifyDto.VendorName = user?.FullName ?? string.Empty;

            var vendorNotification = await _notificationService.SendNotificationAsync(
                vendorId,
                NotificationTitles.VerificationSubmitted,
                "Your verification request was submitted and is waiting for admin review.",
                NotificationTypes.Info,
                vendorVerify.Id,
                "VendorVerification");

            if (vendorNotification.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to notify vendor {VendorId} about submitted verification. Errors: {Errors}",
                    vendorId,
                    string.Join(" | ", vendorNotification.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminIds = admins
                .Where(a => a.Status == UserStatus.Active)
                .Select(a => a.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (adminIds.Count > 0)
            {
                var adminNotification = await _notificationService.SendNotificationToManyAsync(
                    adminIds,
                    NotificationTitles.NewVendorVerification,
                    $"Vendor '{vendorVerifyDto.VendorName}' submitted a verification request.",
                    NotificationTypes.Info,
                    vendorVerify.Id,
                    "VendorVerification");

                if (adminNotification.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to notify admins about vendor verification {VendorVerifyId}. Errors: {Errors}",
                        vendorVerify.Id,
                        string.Join(" | ", adminNotification.Errors.Select(e => $"{e.Code}:{e.Description}")));
                }
            }

            return vendorVerifyDto;
        }

        public async Task<Result<VendorVerifyDTO>> GetVendorVerificationStatusAsync(string vendorId)
        {
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var historySpecification = new VendorVerifyHistoryByVendorSpecification(vendorId);
            var verifications = (await verifyRepo.GetAllAsync(historySpecification)).ToList();

            var verify = verifications.FirstOrDefault(v => v.Status == VerifyStatus.Pending)
                ?? verifications.FirstOrDefault();

            if (verify == null) 
                return Error.NotFound("VendorVerify.NotFound", "No verification request found for this vendor.");

            var user = await _userManager.FindByIdAsync(vendorId);

            var vendorVerifyDto = _mapper.Map<VendorVerifyDTO>(verify);
            vendorVerifyDto.VendorName = user?.FullName ?? string.Empty;

            return vendorVerifyDto;
        }
    }
}
