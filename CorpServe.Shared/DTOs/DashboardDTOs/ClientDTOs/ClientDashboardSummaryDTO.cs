using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.DashboardDTOs.ClientDTOs
{
    public class ClientDashboardSummaryDTO
    {
        public QuickStatsDTO QuickStats { get; set; } = default!;
        public ICollection<RequestActivityDTO> RequestActivities { get; set; } = new List<RequestActivityDTO>();
        public ICollection<CategoryBreakdownDTO> CategoryBreakdowns { get; set; } = new List<CategoryBreakdownDTO>();
        public ICollection<RecentRequestDTO> RecentRequests { get; set; } = new List<RecentRequestDTO>();
        public ICollection<PendingPaymentDTO> PendingPayments { get; set; } = new List<PendingPaymentDTO>();

    }
    public class QuickStatsDTO
    {
        public int ActiveRequests { get; set; }
        public int ActiveRequestsChangeThisWeek { get; set; }
        public int PendingProposals { get; set; }
        public int NewProposalToday { get; set; }
        public decimal TotalSpentEGP { get; set; }
        public decimal TotalSpentChangePercent { get; set; }
        public int CompletedRequests { get; set; }
        public int CompletedRequestsThisMonth { get; set; }

    }
    public class RequestActivityDTO
    {
        public string DayLabel { get; set; } = default!; // Mon, Tue, Wed, etc.
        public DateTime Date { get; set; }
        public int Created { get; set; }
        public int Completed { get; set; }
    }

    public class CategoryBreakdownDTO
    {
        public string Category { get; set; } = default!;
        public int RequestCount { get; set; }
        public decimal Percentage { get; set; }

    }

    public class RecentRequestDTO
    {
        public string RequestId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string RequestCategory { get; set; } = default!;
        public DateTime Deadline { get; set; } = default!;
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
        public string Status { get; set; } = default!; // Pending , Active, Completed
        public string StatusDisplay { get; set; } = default!; // Pending , Active, Completed, Proposal with Count for UI display
        public int? ProposalCount { get; set; }

    }
    public class PendingPaymentDTO
    {
        public string PaymentId { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public string MerchantOrderId { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal Commision { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CheckoutUrl { get; set; }
    }
}
