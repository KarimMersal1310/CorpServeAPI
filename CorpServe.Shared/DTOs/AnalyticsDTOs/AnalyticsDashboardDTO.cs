using System;
using System.Collections.Generic;

namespace CorpServe.Shared.DTOs.AnalyticsDTOs
{
    public enum AnalyticsDateRangePreset
    {
        Last7Days = 1,
        Last30Days = 2,
        Last90Days = 3,
        Custom = 4
    }

    public class AnalyticsFilterDTO
    {
        public AnalyticsDateRangePreset RangePreset { get; set; } = AnalyticsDateRangePreset.Last30Days;
        public DateTime? StartDateUtc { get; set; }
        public DateTime? EndDateUtc { get; set; }
    }

    public class AnalyticsDateRangeDTO
    {
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public string AppliedPreset { get; set; } = default!;
        public int DaysCount { get; set; }
    }

    public class ClientAnaltyicsDashboardDTO
    {
        public AnalyticsDateRangeDTO DateRange { get; set; } = default!;
        public AnalyticsOverviewDTO Overview { get; set; } = default!;
        public ICollection<AnalyticsTrendPointDTO> SpendingTrend { get; set; } = new List<AnalyticsTrendPointDTO>();
        public ICollection<AnalyticsCategorySpendingDTO> SpendingByCategory { get; set; } = new List<AnalyticsCategorySpendingDTO>();
        public ICollection<AnalyticsRequestStatusDTO> RequestStatusBreakdown { get; set; } = new List<AnalyticsRequestStatusDTO>();
        public ICollection<AnalyticsResponseTimeBucketDTO> VendorResponseTime { get; set; } = new List<AnalyticsResponseTimeBucketDTO>();
        public ICollection<AnalyticsTopVendorDTO> TopVendorsUsed { get; set; } = new List<AnalyticsTopVendorDTO>();
    }

    public class AnalyticsOverviewDTO
    {
        public int TotalRequests { get; set; }
        public int TotalRequestsChange { get; set; }
        public decimal AvgResponseTimeDays { get; set; }
        public decimal AvgResponseTimeChangeDays { get; set; }
        public decimal TotalSpentEGP { get; set; }
        public decimal TotalSpentChangePercent { get; set; }
        public decimal ServiceSuccessRatePercent { get; set; }
        public decimal ServiceSuccessRateChangePercent { get; set; }
    }

    public class AnalyticsTrendPointDTO
    {
        public DateTime DateUtc { get; set; }
        public string Label { get; set; } = default!;
        public decimal AmountEGP { get; set; }
    }

    public class AnalyticsCategorySpendingDTO
    {
        public string CategoryName { get; set; } = default!;
        public decimal AmountEGP { get; set; }
        public decimal Percentage { get; set; }
    }

    public class AnalyticsRequestStatusDTO
    {
        public string Status { get; set; } = default!;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class AnalyticsResponseTimeBucketDTO
    {
        public string Bucket { get; set; } = default!;
        public int Count { get; set; }
    }

    public class AnalyticsTopVendorDTO
    {
        public int Rank { get; set; }
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public int ServicesCount { get; set; }
        public decimal AverageRating { get; set; }
        public decimal TotalSpentEGP { get; set; }
    }

}
