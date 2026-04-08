using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.PaymentDTOs;

namespace CorpServe.Services.Abstraction
{
    public interface IPaymentService
    {
        Task<Result<PaymentCheckoutResponseDTO>> PreparePaymentForCompletedRequestAsync(string requestId, string clientId);
        Task<Result<PaymentCheckoutResponseDTO>> CreatePaymobIntentionAndGetRedirectAsync(string paymentId, string clientId);
        Task<Result<bool>> HandleWebhookAsync(string payload, string? hmac);
        Task<Result<bool>> HasUnpaidCompletedRequestsAsync(string clientId);
        Task<Result<IEnumerable<PaymentSummaryDTO>>> GetPendingPaymentsForClientAsync(string clientId);
        Task<Result<IEnumerable<PaymentSummaryDTO>>> GetPaymentsForClientAsync(string clientId);
        Task<Result<IEnumerable<PaymentSummaryDTO>>> GetPaymentsForVendorAsync(string vendorId);
        Task<Result<IEnumerable<AdminPaymentSummaryDTO>>> GetPaymentsForAdminAsync();
        Task<Result<RequestPaymentStatusDTO>> GetRequestPaymentStatusAsync(string requestId, string userId, bool isAdmin, bool isClient, bool isVendor);
        Task<Result<bool>> MarkPayoutPaidAsync(string paymentId, string payoutReference);
        Task<Result<bool>> MarkPayoutFailedAsync(string paymentId, string reason);
    }
}
