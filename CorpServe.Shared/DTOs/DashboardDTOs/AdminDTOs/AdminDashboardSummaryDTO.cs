using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.DashboardDTOs.AdminDTOs
{
    public class AdminDashboardSummaryDTO
    {
        public AdminQuickStatsDTO AdminQuickStats { get; set; } = default!;
        public ICollection<PlatformActivityDTO> PlatformActivities { get; set; } = default!;
        public ICollection<UserDistributionDTO> UserDistributions { get; set; } = default!;
        public ICollection<ServiceCategoryDTO> ServiceCategories { get; set; } = default!;
        public ICollection<PendingVendorApprovalDTO> PendingVendorApprovals { get; set; } = default!;
        public ICollection<VendorPerformanceDTO> VendorPerformance { get; set; } = default!; // last 30 days performance of top 5 vendors based on completed requests
    }
    public class AdminQuickStatsDTO
    {
        public int TotalUsers { get; set; }
        public int TotalUsersChangeThisWeek { get; set; }
        public int TotalActiveRequests { get; set; }
        public int TotalActiveRequestsChangeToday { get; set; }
        public decimal PlatformRevenue { get; set; }
        public int RevenueMoMPercent { get; set; } // change in revenue between the current month and the previous month
        public int SLABreachRiskCount { get; set; }
        public bool SlaBreachNeedAttention { get; set; }
    }
    public class PlatformActivityDTO
    {
        public int Day { get; set; }  // 1–30
        public string MonthLabel { get; set; } = default!;
        public int Requests { get; set; }
        public int Signups { get; set; }
        public int Completed { get; set; }
    }
    public class UserDistributionDTO
    {
        public int TotalUsers { get; set; }
        public double ClientsPercent { get; set; } 
        public double VendorsPercent { get; set; }
        public double AdminsPercent { get; set; }
    }
    public class ServiceCategoryDTO
    {
        public string CategoryName { get; set; } = default!;
        public double Percent { get; set; }
    }
    public class PendingVendorApprovalDTO
    {
        public string VendorId { get; set; } = default!;
        public string ProfilePicUrl { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string CategoryName { get; set; } = default!;
        public DateTime SubmittedAt { get; set; }

    }
    public class VendorPerformanceDTO
    {
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public int CompletedRequests { get; set; }
    }
}
