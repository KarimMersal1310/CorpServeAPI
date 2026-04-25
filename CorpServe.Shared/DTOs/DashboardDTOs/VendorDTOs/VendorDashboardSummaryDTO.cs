using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.DashboardDTOs.VendorDTOs
{
    public class VendorDashboardSummaryDTO
    {
        public VendorQuickStatsDTO VendorQuickStats { get; set; } = default!;
        public ICollection<EarningsOverTimeDTO> earningsOverTimes { get; set; } = new List<EarningsOverTimeDTO>();
        public VendorProposalWinRateDTO proposalWinRate { get; set; } = default!;
        public ICollection<ActiveContractDTO> activeContracts { get; set; } = new List<ActiveContractDTO>();
        public ICollection<UpcomingDeadlineDTO> upcomingDeadlines { get; set; } = new List<UpcomingDeadlineDTO>();
    }
    public class VendorQuickStatsDTO
    {
        public int ActiveContracts { get; set; }
        public int ActiveContractsChangeThisWeek { get; set; }
        public decimal RevenueThisMonthEGP { get; set; }
        public int RevenuePercent { get; set; }
        public decimal AvgRating { get; set; }
        public int TotalRatingCount { get; set; }
    }

    public class EarningsOverTimeDTO
    {
        public string MonthLabel { get; set; } = default!;   // "Nov", "Dec", ...
        public DateTime Month { get; set; }
        public decimal BilledEGP { get; set; }
        public decimal ReceivedEGP { get; set; }
    }

    public class VendorProposalWinRateDTO
    {
        public int Submitted { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public decimal WinRatePercent { get; set; }
    }

    public class ActiveContractDTO
    {
        public string SLAContractId { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public DateTime Deadline { get; set; }
        public int ProgressPercent { get; set; }
        public string ContractStatus { get; set; } = default!;  // Inprogress , Delayed , Completed
    }
    public class UpcomingDeadlineDTO
    {
        public string SLAContractId { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public DateTime Deadline { get; set; }
        public string UrgencyLevel { get; set; } = default!; // "Red", "Orange", "Green", "Blue" based on how close the deadline is
    }
}
