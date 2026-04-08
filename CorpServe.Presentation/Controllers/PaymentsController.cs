using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.PaymentDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CorpServe.Presentation.Controllers
{
    public class PaymentsController : ApiBaseController
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize(Roles = "Client")]
        [HttpPost("requests/{requestId}/checkout")]
        public async Task<ActionResult<PaymentCheckoutResponseDTO>> StartCheckout(string requestId)
        {
            var userId = GetUserIdFromToken();
            var prepared = await _paymentService.PreparePaymentForCompletedRequestAsync(requestId, userId);
            if (prepared.IsFailure)
                return HandleResult(prepared);

            var checkout = await _paymentService.CreatePaymobIntentionAndGetRedirectAsync(prepared.Value.PaymentId, userId);
            return HandleResult(checkout);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("my/pending")]
        public async Task<ActionResult<IEnumerable<PaymentSummaryDTO>>> GetMyPendingPayments()
        {
            var result = await _paymentService.GetPendingPaymentsForClientAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("my/history")]
        public async Task<ActionResult<IEnumerable<PaymentSummaryDTO>>> GetMyPaymentsHistory()
        {
            var result = await _paymentService.GetPaymentsForClientAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor/receivables")]
        public async Task<ActionResult<IEnumerable<PaymentSummaryDTO>>> GetVendorReceivables()
        {
            var result = await _paymentService.GetPaymentsForVendorAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<ActionResult<IEnumerable<AdminPaymentSummaryDTO>>> GetAllPaymentsForAdmin()
        {
            var result = await _paymentService.GetPaymentsForAdminAsync();
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{paymentId}/payout/mark-paid")]
        public async Task<ActionResult<bool>> MarkPayoutPaid(string paymentId, [FromQuery] string payoutReference)
        {
            var result = await _paymentService.MarkPayoutPaidAsync(paymentId, payoutReference);
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{paymentId}/payout/mark-failed")]
        public async Task<ActionResult<bool>> MarkPayoutFailed(string paymentId, [FromQuery] string reason)
        {
            var result = await _paymentService.MarkPayoutFailedAsync(paymentId, reason);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpPost("webhooks/paymob")]
        public async Task<ActionResult<bool>> PaymobWebhook([FromBody] JsonElement payload)
        {
            var hmac = HttpContext.Request.Query["hmac"].ToString();
            var result = await _paymentService.HandleWebhookAsync(payload.GetRawText(), hmac);
            if (result.IsFailure)
                return HandleResult(result);

            return Ok(true);
        }

        [Authorize]
        [HttpGet("requests/{requestId}/status")]
        public async Task<ActionResult<RequestPaymentStatusDTO>> GetRequestPaymentStatus(string requestId)
        {
            var userId = GetUserIdFromToken();
            var isAdmin = User.IsInRole("Admin");
            var isClient = User.IsInRole("Client");
            var isVendor = User.IsInRole("Vendor");

            var result = await _paymentService.GetRequestPaymentStatusAsync(requestId, userId, isAdmin, isClient, isVendor);
            return HandleResult(result);
        }
    }
}
