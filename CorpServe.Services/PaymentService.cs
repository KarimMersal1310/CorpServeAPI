using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Payments;
using CorpServe.Services.Specifications;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.PaymentDTOs;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CorpServe.Services
{
    public class PaymentService : IPaymentService
    {
        private const decimal CommissionRate = 0.07m;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymobClient _paymobClient;
        private readonly PaymobOptions _paymobOptions;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IPaymobClient paymobClient,
            IOptions<PaymobOptions> paymobOptions,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _paymobClient = paymobClient;
            _paymobOptions = paymobOptions.Value;
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<PaymentCheckoutResponseDTO>> PreparePaymentForCompletedRequestAsync(string requestId, string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Payment.InvalidInput", "Client and request IDs are required.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(new RequestByIdForClientSpecification(requestId, clientId));
            if (request is null)
                return Error.NotFound("Payment.RequestNotFound", "Request not found.");

            if (request.RequestStatus != RequestStatus.Completed || request.SLAContract is null)
                return Error.Validation("Payment.RequestNotCompleted", "Payment can only be prepared for completed requests with SLA.");

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var existing = await paymentRepo.GetByIdAsync(new PaymentByRequestIdSpecification(requestId));
            if (existing is not null)
            {
                if (existing.PaymentStatus == PaymentStatus.Completed)
                    return Error.Conflict("Payment.AlreadyCompleted", "Payment is already completed for this request.");

                return MapCheckoutResponse(existing);
            }

            var amount = request.SLAContract.ContractPrice;
            var commision = Math.Round(amount * CommissionRate, 2, MidpointRounding.AwayFromZero);
            var total = amount + commision;

            var payment = new Payment
            {
                RequestId = request.Id,
                ClientId = request.ClientId,
                VendorId = request.SLAContract.VendorId,
                PaymentStatus = PaymentStatus.Pending,
                PayoutStatus = PayoutStatus.NotStarted,
                CreatedAt = DateTime.UtcNow,
                Amount = amount,
                Commision = commision,
                TotalAmount = total,
                VendorNetAmount = amount,
                MerchantOrderId = BuildInvoiceNumber(requestId)
            };

            await paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            await NotifyPaymentDueAsync(payment, request.Title);

            return MapCheckoutResponse(payment);
        }

        public async Task<Result<PaymentCheckoutResponseDTO>> CreatePaymobIntentionAndGetRedirectAsync(string paymentId, string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(paymentId))
                return Error.Validation("Payment.InvalidInput", "Client and payment IDs are required.");

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payment = await paymentRepo.GetByIdAsync(new PaymentByIdForClientSpecification(paymentId, clientId));
            if (payment is null)
                return Error.NotFound("Payment.NotFound", "Payment not found.");

            if (payment.PaymentStatus == PaymentStatus.Completed)
                return Error.Conflict("Payment.AlreadyCompleted", "Payment is already completed.");

            if (payment.PaymentStatus == PaymentStatus.Rejected)
            {
                // Rejected payments can be retried with a fresh intention/checkout URL.
                payment.PaymentStatus = PaymentStatus.Pending;
                payment.PayoutStatus = PayoutStatus.NotStarted;
                payment.WebhookRawStatus = null;
                payment.WebhookReceivedAt = null;
                payment.PaymobTransactionId = null;
                payment.FailureReason = null;
                payment.PaidAt = null;
                payment.PaymobIntentionId = null;
                payment.ClientSecret = null;
                payment.CheckoutUrl = null;
                payment.PayoutReference = null;
                payment.PayoutCompletedAt = null;
                payment.PayoutFailureReason = null;
            }

            if (payment.PaymentStatus == PaymentStatus.Pending
                && !string.IsNullOrWhiteSpace(payment.CheckoutUrl)
                && !string.IsNullOrWhiteSpace(payment.PaymobIntentionId))
                return MapCheckoutResponse(payment);

            var request = new PaymobIntentionRequest
            {
                Amount = ToCents(payment.TotalAmount),
                Currency = _paymobOptions.Currency,
                SpecialReference = payment.MerchantOrderId,
                NotificationUrl = _paymobOptions.WebhookUrl,
                RedirectionUrl = _paymobOptions.SuccessRedirectUrl,
                PaymentMethods = _paymobOptions.PaymentMethodIntegrationIds,
                BillingData = new Dictionary<string, string>
                {
                    ["first_name"] = "CorpServe",
                    ["last_name"] = "Client",
                    ["email"] = "client@corpserve.local",
                    ["phone_number"] = "NA"
                },
                Extras = new Dictionary<string, string>
                {
                    ["request_id"] = payment.RequestId,
                    ["payment_id"] = payment.Id,
                    ["gross_amount"] = payment.Amount.ToString("0.00"),
                    ["commission_amount"] = payment.Commision.ToString("0.00"),
                    ["vendor_net_amount"] = payment.VendorNetAmount.ToString("0.00")
                },
                Items =
                [
                    new PaymobItem
                    {
                        Name = $"Request {payment.RequestId}",
                        Amount = ToCents(payment.TotalAmount),
                        Description = "CorpServe completed SLA payment",
                        Quantity = 1
                    }
                ]
            };

            var intentionOutcome = await _paymobClient.CreateIntentionAsync(request);
            if (!intentionOutcome.IsSuccess)
            {
                var detail = string.IsNullOrWhiteSpace(intentionOutcome.ErrorDetail)
                    ? "Failed to create Paymob intention. Check API logs and Paymob dashboard configuration."
                    : intentionOutcome.ErrorDetail;
                return Error.BadGateway("Payment.IntentionFailed", detail);
            }

            var response = intentionOutcome.Response!;
            payment.PaymobIntentionId = response.Id;
            payment.ClientSecret = response.ClientSecret;
            payment.CheckoutUrl = BuildCheckoutUrl(response.ClientSecret!);

            paymentRepo.Update(payment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Paymob intention created for PaymentId {PaymentId}, MerchantOrderId {MerchantOrderId}", payment.Id, payment.MerchantOrderId);

            return MapCheckoutResponse(payment);
        }

        public async Task<Result<bool>> HandleWebhookAsync(string payload, string? hmac)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return Error.Validation("Payment.WebhookPayloadRequired", "Webhook payload is required.");

            var validHmac = VerifyHmac(payload, hmac);
            if (!validHmac)
            {
                if (string.Equals(_paymobOptions.Mode, "Test", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Paymob webhook HMAC validation failed in test mode. Continuing for sandbox flow.");
                }
                else
                {
                    return Error.Unauthorized("Payment.InvalidWebhookHmac", "Invalid webhook signature.");
                }
            }

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var merchantOrderId = ReadString(root, "obj", "order", "merchant_order_id")
                                  ?? ReadString(root, "obj", "merchant_order_id")
                                  ?? ReadString(root, "merchant_order_id");

            if (string.IsNullOrWhiteSpace(merchantOrderId))
                return Error.Validation("Payment.MerchantOrderMissing", "Merchant order ID is required in webhook payload.");

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payment = await paymentRepo.GetByIdAsync(new PaymentByMerchantOrderIdSpecification(merchantOrderId));
            if (payment is null)
                return Error.NotFound("Payment.NotFound", "Payment for merchant order was not found.");

            if (payment.PaymentStatus == PaymentStatus.Completed || payment.PaymentStatus == PaymentStatus.Rejected)
                return true;

            var success = ReadBool(root, "obj", "success")
                          ?? ReadBool(root, "success")
                          ?? !(ReadBool(root, "obj", "pending") ?? ReadBool(root, "pending") ?? true);
            var rawStatus = ReadString(root, "obj", "status")
                            ?? ReadString(root, "obj", "pending")
                            ?? (success ? "success" : "failed");

            if (!success)
            {
                var normalizedStatus = (rawStatus ?? string.Empty).Trim().ToLowerInvariant();
                if (normalizedStatus is "success" or "succeeded" or "completed" or "paid" or "captured")
                    success = true;
            }

            payment.WebhookRawStatus = rawStatus;
            payment.WebhookReceivedAt = DateTime.UtcNow;
            payment.PaymobTransactionId = ReadString(root, "obj", "id") ?? ReadString(root, "id");

            if (success)
            {
                payment.PaymentStatus = PaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;
                payment.FailureReason = null;
                payment.PayoutStatus = PayoutStatus.Processing;
                payment.PayoutReference = $"MANUAL_{payment.Id}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                payment.PayoutCompletedAt = DateTime.UtcNow;
                payment.PayoutStatus = PayoutStatus.Paid;
                payment.PayoutFailureReason = null;
            }
            else
            {
                payment.PaymentStatus = PaymentStatus.Rejected;
                payment.FailureReason = ReadString(root, "obj", "data", "message")
                                        ?? ReadString(root, "obj", "source_data", "sub_type")
                                        ?? "Payment failed";
                payment.PayoutStatus = PayoutStatus.Failed;
                payment.PayoutFailureReason = "Payment was not successful; payout is not applicable.";
            }

            paymentRepo.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            await NotifyPaymentStatusUpdatesAsync(payment);

            _logger.LogInformation(
                "Payment webhook processed. PaymentId: {PaymentId}, MerchantOrderId: {MerchantOrderId}, Status: {Status}",
                payment.Id,
                payment.MerchantOrderId,
                payment.PaymentStatus);

            return true;
        }

        public async Task<Result<bool>> HasUnpaidCompletedRequestsAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Validation("Payment.ClientRequired", "Client identity is required.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var hasUnpaid = await requestRepo.AnyAsync(r =>
                r.ClientId == clientId
                && r.RequestStatus == RequestStatus.Completed
                && r.SLAContract != null
                && (r.Payment == null || r.Payment.PaymentStatus != PaymentStatus.Completed));

            return hasUnpaid;
        }

        public async Task<Result<IEnumerable<PaymentSummaryDTO>>> GetPendingPaymentsForClientAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Validation("Payment.ClientRequired", "Client identity is required.");

            await EnsurePaymentsExistForCompletedRequestsAsync(clientId);

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payments = await paymentRepo.GetAllAsync(new PendingPaymentsForClientSpecification(clientId));

            return payments.Select(p => new PaymentSummaryDTO
            {
                PaymentId = p.Id,
                RequestId = p.RequestId,
                RequestTitle = p.Request?.Title ?? string.Empty,
                MerchantOrderId = p.MerchantOrderId,
                PaymentStatus = p.PaymentStatus.ToString(),
                PayoutStatus = p.PayoutStatus.ToString(),
                Amount = p.Amount,
                Commision = p.Commision,
                VendorNetAmount = p.VendorNetAmount,
                TotalAmount = p.TotalAmount,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                PayoutReference = p.PayoutReference,
                PayoutCompletedAt = p.PayoutCompletedAt,
                CheckoutUrl = p.CheckoutUrl
            }).ToList();
        }

        public async Task<Result<IEnumerable<PaymentSummaryDTO>>> GetPaymentsForClientAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Validation("Payment.ClientRequired", "Client identity is required.");

            await EnsurePaymentsExistForCompletedRequestsAsync(clientId);

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payments = await paymentRepo.GetAllAsync(new PaymentsForClientSpecification(clientId));

            return payments.Select(p => new PaymentSummaryDTO
            {
                PaymentId = p.Id,
                RequestId = p.RequestId,
                RequestTitle = p.Request?.Title ?? string.Empty,
                MerchantOrderId = p.MerchantOrderId,
                PaymentStatus = p.PaymentStatus.ToString(),
                PayoutStatus = p.PayoutStatus.ToString(),
                Amount = p.Amount,
                Commision = p.Commision,
                VendorNetAmount = p.VendorNetAmount,
                TotalAmount = p.TotalAmount,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                PayoutReference = p.PayoutReference,
                PayoutCompletedAt = p.PayoutCompletedAt,
                CheckoutUrl = p.CheckoutUrl
            }).ToList();
        }

        public async Task<Result<IEnumerable<PaymentSummaryDTO>>> GetPaymentsForVendorAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Error.Validation("Payment.VendorRequired", "Vendor identity is required.");

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payments = await paymentRepo.GetAllAsync(new PaymentsForVendorSpecification(vendorId));

            return payments.Select(p => new PaymentSummaryDTO
            {
                PaymentId = p.Id,
                RequestId = p.RequestId,
                RequestTitle = p.Request?.Title ?? string.Empty,
                MerchantOrderId = p.MerchantOrderId,
                PaymentStatus = p.PaymentStatus.ToString(),
                PayoutStatus = p.PayoutStatus.ToString(),
                Amount = p.Amount,
                Commision = p.Commision,
                VendorNetAmount = p.VendorNetAmount,
                TotalAmount = p.TotalAmount,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                PayoutReference = p.PayoutReference,
                PayoutCompletedAt = p.PayoutCompletedAt,
                CheckoutUrl = p.CheckoutUrl
            }).ToList();
        }

        public async Task<Result<IEnumerable<AdminPaymentSummaryDTO>>> GetPaymentsForAdminAsync()
        {
            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payments = await paymentRepo.GetAllAsync(new PaymentsForAdminSpecification());

            return payments.Select(p => new AdminPaymentSummaryDTO
            {
                PaymentId = p.Id,
                RequestId = p.RequestId,
                RequestTitle = p.Request?.Title ?? string.Empty,
                ClientId = p.ClientId,
                ClientName = p.Client?.FullName ?? p.Client?.UserName ?? p.ClientId,
                VendorId = p.VendorId,
                VendorName = p.Vendor?.FullName ?? p.Vendor?.UserName ?? p.VendorId,
                MerchantOrderId = p.MerchantOrderId,
                PaymentStatus = p.PaymentStatus.ToString(),
                PayoutStatus = p.PayoutStatus.ToString(),
                Amount = p.Amount,
                Commision = p.Commision,
                VendorNetAmount = p.VendorNetAmount,
                TotalAmount = p.TotalAmount,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                PayoutReference = p.PayoutReference,
                PayoutCompletedAt = p.PayoutCompletedAt
            }).ToList();
        }

        public async Task<Result<RequestPaymentStatusDTO>> GetRequestPaymentStatusAsync(string requestId, string userId, bool isAdmin, bool isClient, bool isVendor)
        {
            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(userId))
                return Error.Validation("Payment.InvalidInput", "Request and user IDs are required.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(new RequestByIdWithSlaSpecification(requestId));
            if (request is null)
                return Error.NotFound("Payment.RequestNotFound", "Request not found.");

            var authorized = isAdmin
                             || (isClient && request.ClientId == userId)
                             || (isVendor && request.SLAContract is not null && request.SLAContract.VendorId == userId);

            if (!authorized)
                return Error.forbidden("Payment.Forbidden", "You are not allowed to view this payment status.");

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payment = await paymentRepo.GetByIdAsync(new RequestPaymentStatusSpecification(requestId));

            if (payment is null)
            {
                return new RequestPaymentStatusDTO
                {
                    RequestId = requestId,
                    PaymentStatus = "Unpaid",
                    PayoutStatus = PayoutStatus.NotStarted.ToString()
                };
            }

            return new RequestPaymentStatusDTO
            {
                RequestId = payment.RequestId,
                PaymentId = payment.Id,
                PaymentStatus = payment.PaymentStatus.ToString(),
                PayoutStatus = payment.PayoutStatus.ToString(),
                Amount = payment.Amount,
                Commision = payment.Commision,
                TotalAmount = payment.TotalAmount,
                VendorNetAmount = payment.VendorNetAmount,
                PaidAt = payment.PaidAt,
                FailureReason = payment.FailureReason,
                PayoutReference = payment.PayoutReference,
                PayoutCompletedAt = payment.PayoutCompletedAt,
                PayoutFailureReason = payment.PayoutFailureReason,
                CheckoutUrl = payment.CheckoutUrl
            };
        }

        public async Task<Result<bool>> MarkPayoutPaidAsync(string paymentId, string payoutReference)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return Error.Validation("Payment.PaymentRequired", "Payment ID is required.");

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payment = await paymentRepo.GetByIdAsync(paymentId);
            if (payment is null)
                return Error.NotFound("Payment.NotFound", "Payment not found.");

            if (payment.PaymentStatus != PaymentStatus.Completed)
                return Error.Validation("Payment.NotCompleted", "Payout can only be marked for completed payments.");

            payment.PayoutStatus = PayoutStatus.Paid;
            payment.PayoutReference = string.IsNullOrWhiteSpace(payoutReference) ? payment.PayoutReference : payoutReference.Trim();
            payment.PayoutCompletedAt = DateTime.UtcNow;
            payment.PayoutFailureReason = null;

            paymentRepo.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            await NotifyPayoutStatusUpdatesAsync(payment);
            return true;
        }

        public async Task<Result<bool>> MarkPayoutFailedAsync(string paymentId, string reason)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return Error.Validation("Payment.PaymentRequired", "Payment ID is required.");

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var payment = await paymentRepo.GetByIdAsync(paymentId);
            if (payment is null)
                return Error.NotFound("Payment.NotFound", "Payment not found.");

            payment.PayoutStatus = PayoutStatus.Failed;
            payment.PayoutFailureReason = string.IsNullOrWhiteSpace(reason) ? "Payout failed." : reason.Trim();
            payment.PayoutCompletedAt = null;

            paymentRepo.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            await NotifyPayoutStatusUpdatesAsync(payment);
            return true;
        }

        private Result<PaymentCheckoutResponseDTO> MapCheckoutResponse(Payment payment)
        {
            return new PaymentCheckoutResponseDTO
            {
                PaymentId = payment.Id,
                RequestId = payment.RequestId,
                MerchantOrderId = payment.MerchantOrderId,
                PaymentStatus = payment.PaymentStatus.ToString(),
                Amount = payment.Amount,
                Commision = payment.Commision,
                TotalAmount = payment.TotalAmount,
                VendorNetAmount = payment.VendorNetAmount,
                PayoutStatus = payment.PayoutStatus.ToString(),
                CheckoutUrl = payment.CheckoutUrl ?? string.Empty
            };
        }

        private string BuildCheckoutUrl(string clientSecret)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_paymobOptions.HostedCheckoutBaseUrl)
                ? _paymobOptions.BaseUrl.TrimEnd('/')
                : _paymobOptions.HostedCheckoutBaseUrl.TrimEnd('/');

            return $"{baseUrl}/unifiedcheckout/?publicKey={Uri.EscapeDataString(_paymobOptions.PublicKey)}&clientSecret={Uri.EscapeDataString(clientSecret)}";
        }

        private bool VerifyHmac(string payload, string? receivedHmac)
        {
            if (string.IsNullOrWhiteSpace(receivedHmac))
                return false;

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_paymobOptions.WebhookHmacSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computed = Convert.ToHexString(hash).ToLowerInvariant();
            return string.Equals(computed, receivedHmac.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
        }

        private static long ToCents(decimal value)
            => (long)Math.Round(value * 100m, 0, MidpointRounding.AwayFromZero);

        private static string BuildInvoiceNumber(string requestId)
            => $"INV-{requestId.Trim().ToUpperInvariant()}";

        private async Task EnsurePaymentsExistForCompletedRequestsAsync(string clientId)
        {
            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var completedUnpaidRequests = await requestRepo.GetAllAsync(new CompletedUnpaidRequestsForClientSpecification(clientId));
            var requestsMissingPayment = completedUnpaidRequests
                .Where(r => r.Payment is null && r.SLAContract is not null)
                .ToList();

            if (requestsMissingPayment.Count == 0)
                return;

            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            foreach (var request in requestsMissingPayment)
            {
                // Double-check to avoid duplicate creation during concurrent requests.
                var alreadyExists = await paymentRepo.AnyAsync(p => p.RequestId == request.Id);
                if (alreadyExists)
                    continue;

                var amount = request.SLAContract!.ContractPrice;
                var commision = Math.Round(amount * CommissionRate, 2, MidpointRounding.AwayFromZero);
                var total = amount + commision;

                var payment = new Payment
                {
                    RequestId = request.Id,
                    ClientId = request.ClientId,
                    VendorId = request.SLAContract.VendorId,
                    PaymentStatus = PaymentStatus.Pending,
                    PayoutStatus = PayoutStatus.NotStarted,
                    CreatedAt = DateTime.UtcNow,
                    Amount = amount,
                    Commision = commision,
                    TotalAmount = total,
                    VendorNetAmount = amount,
                    MerchantOrderId = BuildInvoiceNumber(request.Id)
                };

                await paymentRepo.AddAsync(payment);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
                foreach (var request in requestsMissingPayment)
                {
                    if (request.SLAContract is null)
                        continue;

                    var created = await paymentRepo.GetByIdAsync(new PaymentByRequestIdSpecification(request.Id));
                    if (created is not null)
                        await NotifyPaymentDueAsync(created, request.Title);
                }
            }
            catch (DbUpdateException ex)
            {
                // If another parallel request created the same payment first, ignore and continue.
                _logger.LogWarning(ex, "Concurrent payment backfill detected for client {ClientId}. Continuing without failure.", clientId);
            }
        }

        private static string? ReadString(JsonElement source, params string[] path)
        {
            var element = TryGetElement(source, path);
            if (element is null)
                return null;

            return element.Value.ValueKind switch
            {
                JsonValueKind.String => element.Value.GetString(),
                JsonValueKind.Number => element.Value.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => null
            };
        }

        private static bool? ReadBool(JsonElement source, params string[] path)
        {
            var element = TryGetElement(source, path);
            if (element is null)
                return null;

            if (element.Value.ValueKind == JsonValueKind.True)
                return true;

            if (element.Value.ValueKind == JsonValueKind.False)
                return false;

            if (element.Value.ValueKind == JsonValueKind.String && bool.TryParse(element.Value.GetString(), out var parsed))
                return parsed;

            return null;
        }

        private static JsonElement? TryGetElement(JsonElement source, params string[] path)
        {
            var current = source;
            foreach (var segment in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
                    return null;

                current = next;
            }

            return current;
        }

        private async Task NotifyPaymentDueAsync(Payment payment, string requestTitle)
        {
            var result = await _notificationService.SendNotificationAsync(
                payment.ClientId,
                NotificationTitles.PaymentDue,
                $"Payment is now due for completed request '{requestTitle}'.",
                NotificationTypes.Warning,
                payment.RequestId,
                "Payment",
                sendEmail: false);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to send payment due notification for PaymentId {PaymentId}.", payment.Id);
            }
        }

        private async Task NotifyPaymentStatusUpdatesAsync(Payment payment)
        {
            if (payment.PaymentStatus == PaymentStatus.Completed)
            {
                await _notificationService.SendNotificationAsync(
                    payment.ClientId,
                    NotificationTitles.PaymentCompleted,
                    $"Payment for request '{payment.Request?.Title ?? payment.RequestId}' completed successfully.",
                    NotificationTypes.Success,
                    payment.RequestId,
                    "Payment",
                    sendEmail: false);

                await _notificationService.SendNotificationAsync(
                    payment.VendorId,
                    NotificationTitles.VendorPayoutAvailable,
                    $"Client payment completed for request '{payment.Request?.Title ?? payment.RequestId}'. Your receivable is now available.",
                    NotificationTypes.Info,
                    payment.RequestId,
                    "Payment",
                    sendEmail: false);

                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count > 0)
                {
                    await _notificationService.SendNotificationToManyAsync(
                        admins.Select(a => a.Id),
                        NotificationTitles.AdminCommissionRecorded,
                        $"Commission recorded for payment '{payment.Id}' ({payment.Commision:0.00} EGP).",
                        NotificationTypes.Info,
                        payment.Id,
                        "Payment",
                        sendEmail: false);
                }
            }
            else if (payment.PaymentStatus == PaymentStatus.Rejected)
            {
                await _notificationService.SendNotificationAsync(
                    payment.ClientId,
                    NotificationTitles.PaymentFailed,
                    $"Payment failed for request '{payment.Request?.Title ?? payment.RequestId}'. Please retry.",
                    NotificationTypes.Error,
                    payment.RequestId,
                    "Payment",
                    sendEmail: false);
            }
        }

        private async Task NotifyPayoutStatusUpdatesAsync(Payment payment)
        {
            if (payment.PayoutStatus == PayoutStatus.Paid)
            {
                await _notificationService.SendNotificationAsync(
                    payment.VendorId,
                    NotificationTitles.VendorPayoutSettled,
                    $"Payout settled for request '{payment.RequestId}'.",
                    NotificationTypes.Success,
                    payment.RequestId,
                    "Payment",
                    sendEmail: false);

                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count > 0)
                {
                    await _notificationService.SendNotificationToManyAsync(
                        admins.Select(a => a.Id),
                        NotificationTitles.PayoutSettled,
                        $"Payout settled for payment '{payment.Id}'.",
                        NotificationTypes.Info,
                        payment.Id,
                        "Payment",
                        sendEmail: false);
                }
            }
            else if (payment.PayoutStatus == PayoutStatus.Failed)
            {
                await _notificationService.SendNotificationAsync(
                    payment.VendorId,
                    NotificationTitles.PayoutFailed,
                    $"Payout failed for request '{payment.RequestId}'.",
                    NotificationTypes.Error,
                    payment.RequestId,
                    "Payment",
                    sendEmail: false);

                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count > 0)
                {
                    await _notificationService.SendNotificationToManyAsync(
                        admins.Select(a => a.Id),
                        NotificationTitles.PayoutFailed,
                        $"Payout failed for payment '{payment.Id}'.",
                        NotificationTypes.Warning,
                        payment.Id,
                        "Payment",
                        sendEmail: false);
                }
            }
        }
    }
}
