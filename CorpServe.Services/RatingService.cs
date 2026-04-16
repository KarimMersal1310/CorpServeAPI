using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.RatingModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.RatingDTOs;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace CorpServe.Services
{
    public class RatingService : IRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RatingService(IUnitOfWork unitOfWork, INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<Result<RatingRequirementDTO>> GetRatingRequirementForRequestAsync(string clientId, string requestId)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Rating.InvalidInput", "Client and request IDs are required.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(new RequestByIdForClientSpecification(requestId, clientId));
            if (request is null)
                return Error.NotFound("Rating.RequestNotFound", "Request not found.");

            var payment = request.Payment;
            if (payment is null || payment.PaymentStatus != PaymentStatus.Completed || request.SLAContract is null)
            {
                return new RatingRequirementDTO
                {
                    RequestId = requestId,
                    IsRequired = false
                };
            }

            var ratingRepo = _unitOfWork.GetRepository<Rating, string>();
            var existing = await ratingRepo.GetByIdAsync(new RatingByRequestIdSpecification(requestId));

            return new RatingRequirementDTO
            {
                RequestId = requestId,
                PaymentId = payment.Id,
                RequestTitle = request.Title,
                VendorId = request.SLAContract.VendorId,
                VendorName = request.SLAContract.Vendor?.FullName ?? request.SLAContract.Vendor?.UserName ?? request.SLAContract.VendorId,
                IsRequired = existing is null
            };
        }

        public async Task<Result<IEnumerable<RatingRequirementDTO>>> GetPendingRatingsForClientAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Validation("Rating.ClientRequired", "Client identity is required.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var requests = await requestRepo.GetAllAsync(new CompletedPaidRequestsForClientSpecification(clientId));

            // We need completed + paid + no rating.
            var result = new List<RatingRequirementDTO>();
            var ratingRepo = _unitOfWork.GetRepository<Rating, string>();
            foreach (var request in requests.Where(r => r.Payment is not null && r.Payment.PaymentStatus == PaymentStatus.Completed && r.SLAContract is not null))
            {
                var hasRating = await ratingRepo.AnyAsync(r => r.RequestId == request.Id);
                if (hasRating)
                    continue;

                result.Add(new RatingRequirementDTO
                {
                    RequestId = request.Id,
                    PaymentId = request.Payment!.Id,
                    RequestTitle = request.Title,
                    VendorId = request.SLAContract!.VendorId,
                    VendorName = request.SLAContract.Vendor?.FullName ?? request.SLAContract.Vendor?.UserName ?? request.SLAContract.VendorId,
                    IsRequired = true
                });
            }

            return result;
        }

        public async Task<Result<RatingSummaryDTO>> SubmitRatingAsync(string clientId, string requestId, SubmitRatingDTO dto)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Rating.InvalidInput", "Client and request IDs are required.");

            if (dto.Stars < 1 || dto.Stars > 5)
                return Error.Validation("Rating.InvalidStars", "Stars must be between 1 and 5.");

            if (!string.IsNullOrWhiteSpace(dto.Comment) && dto.Comment.Trim().Length > 1000)
                return Error.Validation("Rating.CommentTooLong", "Comment must be 1000 characters or less.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(new RequestByIdForClientSpecification(requestId, clientId));
            if (request is null)
                return Error.NotFound("Rating.RequestNotFound", "Request not found.");

            if (request.SLAContract is null || request.Payment is null || request.Payment.PaymentStatus != PaymentStatus.Completed)
                return Error.Validation("Rating.NotAllowed", "Rating is only allowed after successful payment.");

            var ratingRepo = _unitOfWork.GetRepository<Rating, string>();
            var existing = await ratingRepo.GetByIdAsync(new RatingByRequestIdSpecification(requestId));
            if (existing is not null)
                return Error.Conflict("Rating.Locked", "Rating already submitted and locked.");

            var rating = new Rating
            {
                RequestId = request.Id,
                PaymentId = request.Payment.Id,
                ClientId = clientId,
                VendorId = request.SLAContract.VendorId,
                Stars = dto.Stars,
                Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
                IsLocked = true,
                CreatedAt = DateTime.UtcNow
            };

            await ratingRepo.AddAsync(rating);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                request.SLAContract.VendorId,
                "New vendor rating",
                $"You received a {rating.Stars}/5 rating for request '{request.Title}'.",
                NotificationTypes.Info,
                request.Id,
                "Rating",
                sendEmail: false);

            var vendorId = request.SLAContract.VendorId;
            var pics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, new[] { vendorId });
            pics.TryGetValue(vendorId, out var vendorPic);

            return new RatingSummaryDTO
            {
                RequestId = request.Id,
                PaymentId = request.Payment.Id,
                VendorId = vendorId,
                VendorName = request.SLAContract.Vendor?.FullName ?? request.SLAContract.Vendor?.UserName ?? vendorId,
                VendorProfilePictureUrl = string.IsNullOrWhiteSpace(vendorPic) ? null : vendorPic,
                RequestTitle = request.Title,
                Stars = rating.Stars,
                Comment = rating.Comment,
                IsLocked = rating.IsLocked,
                CreatedAt = rating.CreatedAt
            };
        }
    }
}
