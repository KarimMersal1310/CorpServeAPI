using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.RatingModule;
using CorpServe.Domain.Entities.RequestModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.PaymentModule
{
    public class Payment : BaseEntity<string>
    {
        public PaymentStatus PaymentStatus { get; set; }
        public PayoutStatus PayoutStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Amount { get; set; }
        public decimal Commision { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VendorNetAmount { get; set; }
        public string? PayoutReference { get; set; }
        public DateTime? PayoutCompletedAt { get; set; }
        public string? PayoutFailureReason { get; set; }
        public string MerchantOrderId { get; set; } = default!;
        public string? PaymobIntentionId { get; set; }
        public string? ClientSecret { get; set; }
        public string? PaymobTransactionId { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? WebhookRawStatus { get; set; }
        public DateTime? WebhookReceivedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? FailureReason { get; set; }

        #region RelationShips

        #region Client - Payment (1-M)
        public string ClientId { get; set; } = default!;
        public ApplicationUser Client { get; set; } = default!;
        #endregion

        #region Vendor - Payment (1-M)
        public string VendorId { get; set; } = default!;
        public ApplicationUser Vendor { get; set; } = default!;
        #endregion

        #region Request - Payment (1-1)

        public string RequestId { get; set; } = default!;
        public Request Request { get; set; } = default!;

        #endregion

        #region Payment - Rating (1-1)
        public Rating? Rating { get; set; }
        #endregion

        #endregion

    }
}
