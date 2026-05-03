using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AuthDTOs;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CorpServe.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IVendorVerificationQuery _vendorVerificationQuery;

        public UserProfileService(
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IFileStorageService fileStorageService,
            IVendorVerificationQuery vendorVerificationQuery)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _fileStorageService = fileStorageService;
            _vendorVerificationQuery = vendorVerificationQuery;
        }

        public async Task<Result<UserProfileDetailsDTO>> GetMyProfileAsync(string userId)
        {
            return await BuildUserProfileAsync(userId, userId);
        }

        public async Task<Result<UserProfileDetailsDTO>> GetUserProfileAsync(string requesterUserId, string targetUserId)
        {
            return await BuildUserProfileAsync(requesterUserId, targetUserId);
        }

        public async Task<Result<bool>> UpsertProfileAsync(string userId, UpsertUserProfileDTO upsertUserProfileDTO)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("User.Unauthorized", "User identity is required.");

            var user = await _userManager.Users
                .Include(u => u.UserProfile)
                .ThenInclude(p => p.Documents)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            user.UserProfile ??= new UserProfile
            {
                UserId = userId,
                CompanyName = string.Empty,
                CompanyLocation = string.Empty,
                Description = string.Empty,
                ProfilePictureUrl = string.Empty,
                Documents = new List<ProfileDocument>()
            };

            if (upsertUserProfileDTO.CompanyName is not null)
                user.UserProfile.CompanyName = upsertUserProfileDTO.CompanyName.Trim();

            if (upsertUserProfileDTO.CompanyLocation is not null)
                user.UserProfile.CompanyLocation = upsertUserProfileDTO.CompanyLocation.Trim();

            if (upsertUserProfileDTO.ProfilePictureUrl is not null)
                user.UserProfile.ProfilePictureUrl = upsertUserProfileDTO.ProfilePictureUrl.Trim();

            if (upsertUserProfileDTO.ProfilePicture is { Length: > 0 })
            {
                if (!string.IsNullOrWhiteSpace(user.UserProfile.ProfilePictureUrl))
                    await _fileStorageService.DeleteAsync(user.UserProfile.ProfilePictureUrl);

                user.UserProfile.ProfilePictureUrl = await _fileStorageService.UploadAsync(
                    upsertUserProfileDTO.ProfilePicture,
                    "uploads/profile-pictures");
            }

            if (upsertUserProfileDTO.Description is not null)
                user.UserProfile.Description = upsertUserProfileDTO.Description.Trim();

            if (upsertUserProfileDTO.Documents is not null)
            {
                user.UserProfile.Documents ??= new List<ProfileDocument>();
                user.UserProfile.Documents.Clear();

                foreach (var document in upsertUserProfileDTO.Documents
                    .Where(d => !string.IsNullOrWhiteSpace(d.Name)
                        && !string.IsNullOrWhiteSpace(d.DocumentType)
                        && !string.IsNullOrWhiteSpace(d.DocumentUrl)))
                {
                    user.UserProfile.Documents.Add(new ProfileDocument
                    {
                        Name = document.Name.Trim(),
                        DocumentType = document.DocumentType.Trim(),
                        DocumentUrl = document.DocumentUrl.Trim()
                    });
                }
            }

            if (upsertUserProfileDTO.DocumentFiles is { Count: > 0 })
            {
                user.UserProfile.Documents ??= new List<ProfileDocument>();

                foreach (var existingDocument in user.UserProfile.Documents)
                {
                    if (!string.IsNullOrWhiteSpace(existingDocument.DocumentUrl))
                        await _fileStorageService.DeleteAsync(existingDocument.DocumentUrl);
                }

                user.UserProfile.Documents.Clear();

                foreach (var file in upsertUserProfileDTO.DocumentFiles.Where(f => f is { Length: > 0 }))
                {
                    var documentUrl = await _fileStorageService.UploadAsync(file, "uploads/profile-documents");
                    user.UserProfile.Documents.Add(new ProfileDocument
                    {
                        Name = Path.GetFileName(file.FileName),
                        DocumentType = string.IsNullOrWhiteSpace(file.ContentType) ? "file" : file.ContentType,
                        DocumentUrl = documentUrl
                    });
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return updateResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

            await _notificationService.SendNotificationAsync(
                userId,
                NotificationTitles.ProfileUpdated,
                "Your user profile page information was updated successfully.",
                NotificationTypes.Success,
                userId,
                "User",
                sendEmail: false);

            return true;
        }

        private async Task<Result<UserProfileDetailsDTO>> BuildUserProfileAsync(string requesterUserId, string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(requesterUserId) || string.IsNullOrWhiteSpace(targetUserId))
                return Error.Unauthorized("User.Unauthorized", "User identity is required.");

            var targetUser = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.UserProfile)
                .ThenInclude(p => p.Documents)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (targetUser is null)
                return Error.NotFound("User.NotFound", "User not found.");

            var role = (await _userManager.GetRolesAsync(targetUser)).FirstOrDefault() ?? string.Empty;
            var isOwner = string.Equals(requesterUserId, targetUserId, StringComparison.Ordinal);

            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) && !isOwner)
                return Error.NotFound("User.NotFound", "User not found.");

            var response = new UserProfileDetailsDTO
            {
                UserId = targetUser.Id,
                FullName = targetUser.FullName,
                Email = targetUser.Email ?? string.Empty,
                Role = role,
                AccountStatus = targetUser.Status.ToString(),
                IsOwner = isOwner,
                JoinedAt = targetUser.JoinedAt,
                PhoneNumber = isOwner ? (targetUser.PhoneNumber ?? string.Empty) : null,
                // Company / location / bio / avatar URL always come from the UserProfiles table row only.
                // Never surface company name when it duplicates legal/display name (bad legacy data).
                CompanyName = NormalizeCompanyNameForDisplay(targetUser.UserProfile?.CompanyName, targetUser.FullName),
                CompanyLocation = targetUser.UserProfile?.CompanyLocation ?? string.Empty,
                ProfilePictureUrl = targetUser.UserProfile?.ProfilePictureUrl ?? string.Empty,
                Description = targetUser.UserProfile?.Description ?? string.Empty,
                Documents = targetUser.UserProfile?.Documents?.Select(d => new UserProfileDocumentDTO
                {
                    Name = Path.GetFileNameWithoutExtension(d.Name),
                    DocumentType = d.DocumentType,
                    DocumentUrl = d.DocumentUrl
                }).ToList() ?? []
            };

            if (string.Equals(role, "Vendor", StringComparison.OrdinalIgnoreCase))
            {
                var vendorSlaContracts = _userManager.Users
                    .AsNoTracking()
                    .Where(u => u.Id == targetUserId)
                    .SelectMany(u => u.VendorSLAContracts)
                    .Where(s => s.VendorId == targetUserId);

                response.TotalRequestsCount = await vendorSlaContracts.CountAsync();
                response.CompletedRequestsCount = await vendorSlaContracts.CountAsync(s => s.Request.RequestStatus == RequestStatus.Completed);
                response.InProgressRequestsCount = await vendorSlaContracts.CountAsync(s => s.Request.RequestStatus == RequestStatus.Active);
                response.WorkingWithCount = await vendorSlaContracts.Select(s => s.ClientId).Distinct().CountAsync();

                response.ServedCategories = await _userManager.Users
                    .AsNoTracking()
                    .Where(u => u.Id == targetUserId)
                    .SelectMany(u => u.VendorCategories)
                    .Select(vc => vc.Category.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToListAsync();

                response.RatingCount = await _userManager.Users
                    .AsNoTracking()
                    .Where(u => u.Id == targetUserId)
                    .SelectMany(u => u.VendorRatings)
                    .CountAsync();

                if (response.RatingCount > 0)
                {
                    var sumStars = await _userManager.Users
                        .AsNoTracking()
                        .Where(u => u.Id == targetUserId)
                        .SelectMany(u => u.VendorRatings)
                        .SumAsync(r => r.Stars);
                    var avg = (decimal)sumStars / response.RatingCount!.Value;
                    response.VendorStars = decimal.Round(avg, 1, MidpointRounding.AwayFromZero);
                }

                response.IsVendorVerified = await _vendorVerificationQuery.IsVendorApprovedAsync(targetUserId);

                if (isOwner)
                {
                    var earningsSum = await _userManager.Users
                        .AsNoTracking()
                        .Where(u => u.Id == targetUserId)
                        .SelectMany(u => u.VendorPayments)
                        .Where(p => p.PaymentStatus == PaymentStatus.Completed)
                        .SumAsync(p => (decimal?)p.VendorNetAmount);

                    response.TotalEarnings = earningsSum ?? 0;

                    response.RecentRequests = await _userManager.Users
                        .AsNoTracking()
                        .Where(u => u.Id == targetUserId)
                        .SelectMany(u => u.VendorSLAContracts)
                        .Where(s => s.Request.RequestStatus == RequestStatus.Active || s.Request.RequestStatus == RequestStatus.Completed)
                        .OrderByDescending(s => s.Request.CreatedAt)
                        .Take(3)
                        .Select(s => new UserProfileRecentRequestDTO
                        {
                            RequestId = s.RequestId,
                            Category = s.Request.Category.Name,
                            RequestTitle = s.Request.Title,
                            CreatedAt = s.Request.CreatedAt,
                            Status = s.Request.RequestStatus.ToString(),
                            BudgetMin = s.Request.BudgetMin,
                            BudgetMax = s.Request.BudgetMax,
                            Price = s.ContractPrice,
                            ClientName = s.Client.FullName,
                            VendorName = null,
                            Rating = s.Request.Rating != null ? s.Request.Rating.Stars : null,
                            RatingComment = s.Request.Rating != null ? s.Request.Rating.Comment : null
                        })
                        .ToListAsync();
                }

                return response;
            }

            var clientRequests = _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == targetUserId)
                .SelectMany(u => u.Requests)
                .Where(r => r.ClientId == targetUserId);

            response.TotalRequestsCount = await clientRequests.CountAsync();
            response.CompletedRequestsCount = await clientRequests.CountAsync(r => r.RequestStatus == RequestStatus.Completed);
            response.InProgressRequestsCount = await clientRequests.CountAsync(r => r.RequestStatus == RequestStatus.Active);

            var activeSlaVendors = await clientRequests
                .Where(r => r.RequestStatus == RequestStatus.Active && r.SLAContract != null)
                .Select(r => r.SLAContract!.VendorId)
                .ToListAsync();

            var activeProgressVendors = await clientRequests
                .Where(r => r.RequestStatus == RequestStatus.Active && r.RequestProgress.VendorId != null)
                .Select(r => r.RequestProgress.VendorId!)
                .ToListAsync();

            response.WorkingWithCount = activeSlaVendors
                .Concat(activeProgressVendors)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            response.PreferredCategories = await clientRequests
                .GroupBy(r => new { r.CateogryId, r.Category.Name })
                .Select(g => new UserProfileCategoryStatDTO
                {
                    CategoryId = g.Key.CateogryId,
                    CategoryName = g.Key.Name,
                    RequestsCount = g.Count()
                })
                .OrderByDescending(c => c.RequestsCount)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            if (isOwner)
            {
                var budgetCount = await clientRequests.CountAsync();
                if (budgetCount > 0)
                {
                    response.AverageBudget = await clientRequests.AverageAsync(r => (r.BudgetMin + r.BudgetMax) / 2m);
                }

                response.RecentRequests = await _userManager.Users
                    .AsNoTracking()
                    .Where(u => u.Id == targetUserId)
                    .SelectMany(u => u.Requests)
                    .Where(r => r.ClientId == targetUserId && (r.RequestStatus == RequestStatus.Active || r.RequestStatus == RequestStatus.Completed))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(3)
                    .Select(r => new UserProfileRecentRequestDTO
                    {
                        RequestId = r.Id,
                        Category = r.Category.Name,
                        RequestTitle = r.Title,
                        CreatedAt = r.CreatedAt,
                        Status = r.RequestStatus.ToString(),
                        BudgetMin = r.BudgetMin,
                        BudgetMax = r.BudgetMax,
                        Price = r.SLAContract != null ? r.SLAContract.ContractPrice : (decimal?)null,
                        ClientName = null,
                        VendorName = r.SLAContract != null
                            ? r.SLAContract.Vendor.FullName
                            : (r.RequestProgress != null ? r.RequestProgress.Vendor.FullName : null),
                        Rating = r.Rating != null ? r.Rating.Stars : null,
                        RatingComment = r.Rating != null ? r.Rating.Comment : null
                    })
                    .ToListAsync();
            }

            return response;
        }

        public static string NormalizeCompanyNameForDisplay(string? companyName, string? fullName)
        {
            return companyName?.Trim() ?? string.Empty;
        }
    }
}
