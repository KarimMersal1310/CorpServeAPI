using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;

namespace CorpServe.Services.Specifications
{
    public sealed class PaymentByRequestIdSpecification : BaseSpecificactions<Payment, string>
    {
        public PaymentByRequestIdSpecification(string requestId)
            : base(p => p.RequestId == requestId)
        {
            AddInclude(p => p.Request);
        }
    }

    public sealed class PaymentByIdForClientSpecification : BaseSpecificactions<Payment, string>
    {
        public PaymentByIdForClientSpecification(string paymentId, string clientId)
            : base(p => p.Id == paymentId && p.ClientId == clientId)
        {
            AddInclude(p => p.Request);
        }
    }

    public sealed class PendingPaymentsForClientSpecification : BaseSpecificactions<Payment, string>
    {
        public PendingPaymentsForClientSpecification(string clientId)
            : base(p => p.ClientId == clientId && p.PaymentStatus != PaymentStatus.Completed)
        {
            AddInclude(p => p.Request);
            AddOrderByDescending(p => p.CreatedAt);
        }
    }

    public sealed class PaymentsForClientSpecification : BaseSpecificactions<Payment, string>
    {
        public PaymentsForClientSpecification(string clientId)
            : base(p => p.ClientId == clientId)
        {
            AddInclude(p => p.Request);
            AddOrderByDescending(p => p.CreatedAt);
        }
    }

    public sealed class PaymentsForVendorSpecification : BaseSpecificactions<Payment, string>
    {
        public PaymentsForVendorSpecification(string vendorId)
            : base(p => p.VendorId == vendorId)
        {
            AddInclude(p => p.Request);
            AddOrderByDescending(p => p.CreatedAt);
        }
    }

    public sealed class PaymentsForAdminSpecification : BaseSpecificactions<Payment, string>
    {
        public PaymentsForAdminSpecification()
            : base(p => true)
        {
            AddInclude(p => p.Request);
            AddInclude(p => p.Client);
            AddInclude(p => p.Vendor);
            AddOrderByDescending(p => p.CreatedAt);
        }
    }

    public sealed class PaymentByMerchantOrderIdSpecification : BaseSpecificactions<Payment, string>
    {
        public PaymentByMerchantOrderIdSpecification(string merchantOrderId)
            : base(p => p.MerchantOrderId == merchantOrderId)
        {
            AddInclude(p => p.Request);
        }
    }

    public sealed class RequestPaymentStatusSpecification : BaseSpecificactions<Payment, string>
    {
        public RequestPaymentStatusSpecification(string requestId)
            : base(p => p.RequestId == requestId)
        {
            AddInclude(p => p.Request);
        }
    }

    public sealed class OverduePendingPaymentsSpecification : BaseSpecificactions<Payment, string>
    {
        public OverduePendingPaymentsSpecification(DateTime overdueThreshold)
            : base(p => p.PaymentStatus == PaymentStatus.Pending
                && p.CreatedAt <= overdueThreshold
                && p.Client.Status == UserStatus.Active)
        {
            AddInclude(p => p.Client);
            AddInclude(p => p.Request);
        }
    }
}
