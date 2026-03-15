using AutoMapper;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.VendorVerify;
using E_Commerce.Shared.CommonResult;
using EventHub.Domain.Contracts;
using EventHub.Services.Specifications;
using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.AspNetCore.Identity;

namespace CorpServe.Services
{
    public class VendorVerifyService : IVendorVerifyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public VendorVerifyService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _mapper = mapper;
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
                return Error.Failure("VendorVerify.AlreadyExists", "You already have a pending or approved verification request.");
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

            return vendorVerifyDto;
        }

        public async Task<Result<VendorVerifyDTO>> GetVendorVerificationStatusAsync(string vendorId)
        {
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var latestSpecification = new VendorVerifyLatestByVendorSpecification(vendorId);
            var verify = await verifyRepo.GetByIdAsync(latestSpecification);

            if (verify == null) 
                return Error.NotFound("VendorVerify.NotFound", "No verification request found for this vendor.");

            var user = await _userManager.FindByIdAsync(vendorId);

            var vendorVerifyDto = _mapper.Map<VendorVerifyDTO>(verify);
            vendorVerifyDto.VendorName = user?.FullName ?? string.Empty;

            return vendorVerifyDto;
        }
    }
}
