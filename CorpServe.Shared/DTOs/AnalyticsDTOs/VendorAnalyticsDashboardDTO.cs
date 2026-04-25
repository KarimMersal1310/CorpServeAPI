using System;
using System.Collections.Generic;

namespace CorpServe.Shared.DTOs.AnalyticsDTOs
{
    public class VendorAnalyticsDashboardDTO
    {
        public AnalyticsDateRangeDTO DateRange { get; set; } = default!;
        public VendorAnalyticsOverviewDTO Overview { get; set; } = default!;
        public ICollection<VendorAcceptedProposalTrendPointDTO> AcceptedProposalsTrend { get; set; } = new List<VendorAcceptedProposalTrendPointDTO>();
        public VendorClientRetentionDTO ClientRetention { get; set; } = default!;
        public VendorRatingsDistributionDTO RatingsDistribution { get; set; } = default!;
        public ICollection<VendorTopPerformingContractDTO> TopPerformingContracts { get; set; } = new List<VendorTopPerformingContractDTO>();
    }

    public class VendorAnalyticsOverviewDTO
    {
        public int ProposalsSent { get; set; }
        public decimal WinRatePercent { get; set; }
        public decimal WinRateChangePercent { get; set; }
        public decimal AvgContractValueEGP { get; set; }
        public decimal OnTimeDeliveryPercent { get; set; }
        public decimal AvgRating { get; set; }
    }

    public class VendorAcceptedProposalTrendPointDTO
    {
        public DateTime DateUtc { get; set; }
        public string Label { get; set; } = default!;
        public int AcceptedCount { get; set; }
    }

    public class VendorClientRetentionDTO
    {
        public int TotalClientsServed { get; set; }
        public int RepeatClientsCount { get; set; }
        public decimal RepeatClientsPercent { get; set; }
    }

    public class VendorRatingsDistributionDTO
    {
        public decimal AverageRating { get; set; }
        public int TotalRatingsCount { get; set; }
        public ICollection<VendorRatingStarCountDTO> StarsBreakdown { get; set; } = new List<VendorRatingStarCountDTO>();
    }

    public class VendorRatingStarCountDTO
    {
        public int Stars { get; set; }
        public int Count { get; set; }
    }

    public class VendorTopPerformingContractDTO
    {
        public string ClientId { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string Service { get; set; } = default!;
        public decimal ValueEGP { get; set; }
        public DateTime DeliveredAtUtc { get; set; }
        public int DeliveryDeltaDays { get; set; }
        public string DeliveryStatus { get; set; } = default!;
        public decimal Rating { get; set; }
    }
}
