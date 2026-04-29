using System;
using System.Collections.Generic;

namespace CorpServe.Shared.DTOs.AnalyticsDTOs
{
    public class AdminAnalyticsDashboardDTO
    {
        public AnalyticsDateRangeDTO DateRange { get; set; } = default!;
        public AdminAnalyticsOverviewDTO Overview { get; set; } = default!;
        public ICollection<AdminGmvTrendPointDTO> PlatformGmvTrend { get; set; } = new List<AdminGmvTrendPointDTO>();
        public ICollection<AdminServiceCategoryAnalyticsDTO> TopServiceCategories { get; set; } = new List<AdminServiceCategoryAnalyticsDTO>();
        public ICollection<AdminUserGrowthPointDTO> UserGrowth { get; set; } = new List<AdminUserGrowthPointDTO>();
        public AdminRevenueSplitDTO RevenueSplit { get; set; } = default!;
        public ICollection<AdminTopVendorRevenueDTO> TopVendorsByRevenue { get; set; } = new List<AdminTopVendorRevenueDTO>();
        public AdminAnomalyRiskSectionDTO AnomalyRiskFlags { get; set; } = default!;
    }

    public class AdminAnalyticsOverviewDTO
    {
        public decimal GmvEGP { get; set; }
        public decimal GmvChangePercent { get; set; }
        public int ActiveUsersCount { get; set; }
        public decimal AvgTimeToMatchHours { get; set; }
        public decimal SlaCompliancePercent { get; set; }
    }

    public class AdminGmvTrendPointDTO
    {
        public DateTime DateUtc { get; set; }
        public string Label { get; set; } = default!;
        public decimal GmvEGP { get; set; }
    }

    public class AdminServiceCategoryAnalyticsDTO
    {
        public string CategoryName { get; set; } = default!;
        public decimal Percentage { get; set; }
    }

    public class AdminUserGrowthPointDTO
    {
        public DateTime MonthUtc { get; set; }
        public string Label { get; set; } = default!;
        public int ClientsCount { get; set; }
        public int VendorsCount { get; set; }
        public int AdminsCount { get; set; }
    }

    public class AdminRevenueSplitDTO
    {
        public decimal VendorPayoutEGP { get; set; }
        public decimal PlatformFeeEGP { get; set; }
        public decimal VendorPayoutPercent { get; set; }
        public decimal PlatformFeePercent { get; set; }
    }

    public class AdminTopVendorRevenueDTO
    {
        public int Rank { get; set; }
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public decimal RevenueEGP { get; set; }
    }

    public class AdminAnomalyRiskSectionDTO
    {
        public int ActiveFlagsCount { get; set; }
        public ICollection<AdminAnomalyRiskFlagDTO> Flags { get; set; } = new List<AdminAnomalyRiskFlagDTO>();
    }

    public class AdminAnomalyRiskFlagDTO
    {
        public string Type { get; set; } = default!;
        public string Entity { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Severity { get; set; } = default!;
        public DateTime DetectedAtUtc { get; set; }
    }
}
