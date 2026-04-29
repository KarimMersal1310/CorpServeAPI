using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.RatingModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.DashboardDTOs.AdminDTOs;
using CorpServe.Shared.DTOs.DashboardDTOs.ClientDTOs;
using CorpServe.Shared.DTOs.DashboardDTOs.VendorDTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CorpServe.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }



        public async Task<Result<AdminDashboardSummaryDTO>> GetAdminDashboardSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfYesterday = startOfToday.AddDays(-1);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfPreviousMonth = startOfMonth.AddMonths(-1);
            var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;
            var startOfWeek = startOfToday.AddDays(-daysFromMonday);
            var startOfLastWeek = startOfWeek.AddDays(-7);
            var activityStartMonth = startOfMonth.AddMonths(-5);

            var requestsRepo = _unitOfWork.GetRepository<Request, string>();
            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var verifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();

            var requests = await requestsRepo.Query(new AdminDashboardRequestsSpecification()).ToListAsync();
            var completedPayments = await paymentRepo.Query(new AdminDashboardCompletedPaymentsSpecification()).ToListAsync();
            var slaContracts = await slaRepo.Query(new AdminDashboardSlaContractsSpecification()).ToListAsync();
            var pendingVerifications = await verifyRepo.Query(new AdminDashboardPendingVendorVerificationsSpecification()).ToListAsync();

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var vendors = await _userManager.GetUsersInRoleAsync("Vendor");

            var usersById = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);
            foreach (var user in admins)
                usersById[user.Id] = user;
            foreach (var user in clients)
                usersById[user.Id] = user;
            foreach (var user in vendors)
                usersById[user.Id] = user;

            var allUsers = usersById.Values.ToList();
            var totalUsers = allUsers.Count;

            var usersThisWeek = allUsers.Count(u => u.JoinedAt >= startOfWeek);
            var usersLastWeek = allUsers.Count(u => u.JoinedAt >= startOfLastWeek && u.JoinedAt < startOfWeek);

            var activeRequestsToday = requests.Count(r => r.RequestStatus == RequestStatus.Active && r.CreatedAt >= startOfToday);
            var activeRequestsYesterday = requests.Count(r => r.RequestStatus == RequestStatus.Active && r.CreatedAt >= startOfYesterday && r.CreatedAt < startOfToday);

            var currentMonthRevenue = completedPayments
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= startOfMonth)
                .Sum(p => p.Commision);

            var previousMonthRevenue = completedPayments
                .Where(p =>
                {
                    var paidAt = p.PaidAt ?? p.CreatedAt;
                    return paidAt >= startOfPreviousMonth && paidAt < startOfMonth;
                })
                .Sum(p => p.Commision);

            // Breach / overdue attention: persisted Breached, Delayed, or Inprogress within 48h (incl. overdue before monitor runs)
            var breachRiskCount = slaContracts.Count(c =>
                c.SLAStatus != SLAStatus.Completed
                && (
                    c.SLAStatus == SLAStatus.Breached
                    || c.SLAStatus == SLAStatus.Delayed
                    || (c.SLAStatus == SLAStatus.Inprogress && c.Deadline <= now.AddHours(48))));

            var quickStats = new AdminQuickStatsDTO
            {
                TotalUsers = totalUsers,
                TotalUsersChangeThisWeek = usersThisWeek - usersLastWeek,
                TotalActiveRequests = requests.Count(r => r.RequestStatus == RequestStatus.Active),
                TotalActiveRequestsChangeToday = activeRequestsToday - activeRequestsYesterday,
                PlatformRevenue = completedPayments.Sum(p => p.Commision),
                RevenueMoMPercent = previousMonthRevenue == 0
                    ? (currentMonthRevenue > 0 ? 100 : 0)
                    : (int)Math.Round(((currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue) * 100),
                SLABreachRiskCount = breachRiskCount,
                SlaBreachNeedAttention = breachRiskCount > 0
            };

            var platformActivities = Enumerable
                .Range(0, 6)
                .Select(index => activityStartMonth.AddMonths(index))
                .Select((monthStart, index) =>
                {
                    var nextMonth = monthStart.AddMonths(1);
                    return new PlatformActivityDTO
                    {
                        Day = index + 1,
                        MonthLabel = monthStart.ToString("MMM"),
                        Requests = requests.Count(r => r.CreatedAt >= monthStart && r.CreatedAt < nextMonth),
                        Signups = allUsers.Count(u => u.JoinedAt >= monthStart && u.JoinedAt < nextMonth),
                        Completed = requests.Count(r => r.RequestStatus == RequestStatus.Completed && r.CreatedAt >= monthStart && r.CreatedAt < nextMonth)
                    };
                })
                .ToList();

            var userDistributions = new List<UserDistributionDTO>
            {
                new UserDistributionDTO
                {
                    TotalUsers = totalUsers,
                    ClientsPercent = totalUsers == 0 ? 0 : Math.Round((clients.Count * 100d) / totalUsers, 2),
                    VendorsPercent = totalUsers == 0 ? 0 : Math.Round((vendors.Count * 100d) / totalUsers, 2),
                    AdminsPercent = totalUsers == 0 ? 0 : Math.Round((admins.Count * 100d) / totalUsers, 2)
                }
            };

            var totalRequests = requests.Count;
            var serviceCategories = requests
                .GroupBy(r => r.Category?.Name ?? "Uncategorized")
                .Select(group => new ServiceCategoryDTO
                {
                    CategoryName = group.Key,
                    Percent = totalRequests == 0 ? 0 : Math.Round((group.Count() * 100d) / totalRequests, 2)
                })
                .OrderByDescending(x => x.Percent)
                .ToList();

            var categoryIds = pendingVerifications
                .SelectMany(v => v.Vendor?.VendorCategories?.Select(vc => vc.CategoryId) ?? Enumerable.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var categoryNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (categoryIds.Count > 0)
            {
                var categories = await categoryRepo.GetAllAsync(new CategoriesByIdsSpecification(categoryIds));
                categoryNameById = categories.ToDictionary(c => c.Id, c => c.Name, StringComparer.OrdinalIgnoreCase);
            }

            var pendingApprovalVendorsIds = pendingVerifications
                .Select(v => v.VendorId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var profilePics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, pendingApprovalVendorsIds);

            var pendingVendorApprovals = pendingVerifications
                .Take(5)
                .Select(v =>
                {
                    var firstCategoryId = v.Vendor?.VendorCategories?.Select(vc => vc.CategoryId).FirstOrDefault();
                    var categoryName = string.IsNullOrWhiteSpace(firstCategoryId)
                        ? "Uncategorized"
                        : categoryNameById.GetValueOrDefault(firstCategoryId, "Uncategorized");

                    return new PendingVendorApprovalDTO
                    {
                        VendorId = v.VendorId,
                        ProfilePicUrl = profilePics.GetValueOrDefault(v.VendorId, string.Empty),
                        VendorName = v.Vendor?.FullName ?? string.Empty,
                        CategoryName = categoryName,
                        SubmittedAt = v.SubmittedAt
                    };
                })
                .ToList();

            var topVendorCounts = slaContracts
                .Where(c => c.SLAStatus == SLAStatus.Completed && c.CreatedAt >= activityStartMonth)
                .GroupBy(c => c.VendorId, StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    VendorId = group.Key,
                    CompletedRequests = group.Count()
                })
                .OrderByDescending(x => x.CompletedRequests)
                .Take(5)
                .ToList();

            var topVendorIds = topVendorCounts.Select(v => v.VendorId).ToList();
            var vendorNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (topVendorIds.Count > 0)
            {
                var vendorRows = await _userManager.Users
                    .AsNoTracking()
                    .Where(u => topVendorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();

                vendorNameById = vendorRows.ToDictionary(v => v.Id, v => v.FullName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            }

            var vendorPerformance = topVendorCounts
                .Select(v => new VendorPerformanceDTO
                {
                    VendorId = v.VendorId,
                    VendorName = vendorNameById.GetValueOrDefault(v.VendorId, string.Empty),
                    CompletedRequests = v.CompletedRequests
                })
                .ToList();

            return new AdminDashboardSummaryDTO
            {
                AdminQuickStats = quickStats,
                PlatformActivities = platformActivities,
                UserDistributions = userDistributions,
                ServiceCategories = serviceCategories,
                PendingVendorApprovals = pendingVendorApprovals,
                VendorPerformance = vendorPerformance
            };
        }



        public async Task<Result<ClientDashboardSummaryDTO>> GetClientDashboardSummaryAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Validation("Dashboard.ClientRequired", "Client identity is required.");

            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfPreviousMonth = startOfMonth.AddMonths(-1);
            var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;
            var startOfWeek = startOfToday.AddDays(-daysFromMonday);
            var startOfLastWeek = startOfWeek.AddDays(-7);

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();

            var requests = await requestRepo.Query(new ClientDashboardRequestsSpecification(clientId)).ToListAsync();
            var pendingProposals = await proposalRepo.Query(new ClientPendingProposalsForDashboardSpecification(clientId)).ToListAsync();
            var completedPayments = await paymentRepo.Query(new ClientCompletedPaymentsForDashboardSpecification(clientId)).ToListAsync();
            var pendingPayments = await paymentRepo.Query(new ClientPendingPaymentsForDashboardSpecification(clientId)).ToListAsync();

            var activeCurrentWeek = requests.Count(r =>
                r.RequestStatus == RequestStatus.Active
                && r.CreatedAt >= startOfWeek
                && r.CreatedAt < startOfWeek.AddDays(7));

            var activeLastWeek = requests.Count(r =>
                r.RequestStatus == RequestStatus.Active
                && r.CreatedAt >= startOfLastWeek
                && r.CreatedAt < startOfWeek);

            var currentMonthSpent = completedPayments
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= startOfMonth)
                .Sum(p => p.TotalAmount);

            var previousMonthSpent = completedPayments
                .Where(p =>
                {
                    var paidAt = p.PaidAt ?? p.CreatedAt;
                    return paidAt >= startOfPreviousMonth && paidAt < startOfMonth;
                })
                .Sum(p => p.TotalAmount);

            var totalRequestCount = requests.Count;

            var quickStats = new QuickStatsDTO
            {
                ActiveRequests = requests.Count(r => r.RequestStatus == RequestStatus.Active),
                ActiveRequestsChangeThisWeek = activeCurrentWeek - activeLastWeek,
                PendingProposals = pendingProposals.Count,
                NewProposalToday = pendingProposals.Count(p => p.CreatedAt >= startOfToday),
                TotalSpentEGP = completedPayments.Sum(p => p.TotalAmount),
                TotalSpentChangePercent = previousMonthSpent == 0
                    ? (currentMonthSpent > 0 ? 100m : 0m)
                    : decimal.Round(((currentMonthSpent - previousMonthSpent) / previousMonthSpent) * 100m, 2),
                CompletedRequests = requests.Count(r => r.RequestStatus == RequestStatus.Completed),
                CompletedRequestsThisMonth = requests.Count(r =>
                    r.RequestStatus == RequestStatus.Completed
                    && r.CreatedAt >= startOfMonth)
            };

            var requestActivities = Enumerable
                .Range(0, 6)
                .Select(index => startOfMonth.AddMonths(index - 5))
                .Select(monthStart =>
                {
                    var nextMonth = monthStart.AddMonths(1);
                    return new RequestActivityDTO
                    {
                        DayLabel = monthStart.ToString("MMM"),
                        Date = monthStart,
                        Created = requests.Count(r => r.CreatedAt >= monthStart && r.CreatedAt < nextMonth),
                        Completed = requests.Count(r =>
                            r.RequestStatus == RequestStatus.Completed
                            && r.CreatedAt >= monthStart
                            && r.CreatedAt < nextMonth)
                    };
                })
                .ToList();

            var pendingPaymentCards = pendingPayments
                .Select(p => new PendingPaymentDTO
                {
                    PaymentId = p.Id,
                    RequestId = p.RequestId,
                    RequestTitle = p.Request?.Title ?? string.Empty,
                    MerchantOrderId = NormalizeInvoiceNumber(p.MerchantOrderId, p.RequestId),
                    Amount = p.Amount,
                    Commision = p.Commision,
                    TotalAmount = p.TotalAmount,
                    CreatedAt = p.CreatedAt,
                    CheckoutUrl = p.CheckoutUrl
                })
                .ToList();

            var categoryBreakdowns = requests
                .GroupBy(r => r.Category?.Name ?? "Uncategorized")
                .Select(group => new CategoryBreakdownDTO
                {
                    Category = group.Key,
                    RequestCount = group.Count(),
                    Percentage = totalRequestCount == 0
                        ? 0
                        : decimal.Round((group.Count() * 100m) / totalRequestCount, 2)
                })
                .OrderByDescending(c => c.RequestCount)
                .ToList();

            var recentRequests = requests
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r =>
                {
                    var proposalCount = r.Proposals?.Count(p => p.ProposalStatus == ClientStatus.Pending) ?? 0;

                    return new RecentRequestDTO
                    {
                        RequestId = r.Id,
                        Title = r.Title,
                        RequestCategory = r.Category?.Name ?? "Uncategorized",
                        Deadline = r.ExpectedDeadline,
                        BudgetMin = r.BudgetMin,
                        BudgetMax = r.BudgetMax,
                        Status = r.RequestStatus.ToString(),
                        StatusDisplay = BuildStatusDisplay(r.RequestStatus, proposalCount),
                        ProposalCount = proposalCount > 0 ? proposalCount : null
                    };
                })
                .ToList();

            return new ClientDashboardSummaryDTO
            {
                QuickStats = quickStats,
                RequestActivities = requestActivities,
                CategoryBreakdowns = categoryBreakdowns,
                RecentRequests = recentRequests,
                PendingPayments = pendingPaymentCards
            };
        }


        public async Task<Result<VendorDashboardSummaryDTO>> GetVendorDashboardSummaryAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Error.Validation("Dashboard.VendorRequired", "Vendor identity is required.");

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfPreviousMonth = startOfMonth.AddMonths(-1);
            var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;
            var startOfWeek = now.Date.AddDays(-daysFromMonday);
            var startOfLastWeek = startOfWeek.AddDays(-7);

            var contractRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var paymentRepo = _unitOfWork.GetRepository<Payment, string>();
            var ratingRepo = _unitOfWork.GetRepository<Rating, string>();

            var contracts = await contractRepo.Query(new VendorDashboardContractsSpecification(vendorId)).ToListAsync();
            var proposals = await proposalRepo.Query(new VendorDashboardProposalsSpecification(vendorId)).ToListAsync();
            var payments = await paymentRepo.Query(new VendorDashboardPaymentsSpecification(vendorId)).ToListAsync();
            var ratings = await ratingRepo.Query(new VendorDashboardRatingsSpecification(vendorId)).ToListAsync();

            var activeContracts = contracts
                .Where(c => c.SLAStatus != SLAStatus.Completed)
                .ToList();

            var activeContractsThisWeek = activeContracts.Count(c => c.CreatedAt >= startOfWeek && c.CreatedAt < startOfWeek.AddDays(7));
            var activeContractsLastWeek = activeContracts.Count(c => c.CreatedAt >= startOfLastWeek && c.CreatedAt < startOfWeek);
            var activeContractsChangeThisWeek = Math.Max(0, activeContractsThisWeek - activeContractsLastWeek);

            var completedPayments = payments.Where(p => p.PaymentStatus == PaymentStatus.Completed).ToList();
            var currentMonthRevenue = completedPayments
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= startOfMonth)
                .Sum(p => p.VendorNetAmount);
            var previousMonthRevenue = completedPayments
                .Where(p =>
                {
                    var paidAt = p.PaidAt ?? p.CreatedAt;
                    return paidAt >= startOfPreviousMonth && paidAt < startOfMonth;
                })
                .Sum(p => p.VendorNetAmount);

            var acceptedProposals = proposals.Count(p => p.ProposalStatus == ClientStatus.Accepted);
            var rejectedProposals = proposals.Count(p => p.ProposalStatus == ClientStatus.Rejected);
            var submittedProposals = proposals.Count;

            var vendorQuickStats = new VendorQuickStatsDTO
            {
                ActiveContracts = activeContracts.Count,
                ActiveContractsChangeThisWeek = activeContractsChangeThisWeek,
                RevenueThisMonthEGP = currentMonthRevenue,
                RevenuePercent = previousMonthRevenue == 0
                    ? (currentMonthRevenue > 0 ? 100 : 0)
                    : (int)Math.Round(((currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue) * 100),
                AvgRating = ratings.Count == 0 ? 0 : decimal.Round(ratings.Average(r => (decimal)r.Stars), 2),
                TotalRatingCount = ratings.Count
            };

            var earningsOverTimes = Enumerable
                .Range(0, 6)
                .Select(index => startOfMonth.AddMonths(index - 5))
                .Select(monthStart =>
                {
                    var nextMonth = monthStart.AddMonths(1);

                    return new EarningsOverTimeDTO
                    {
                        Month = monthStart,
                        MonthLabel = monthStart.ToString("MMM"),
                        BilledEGP = completedPayments
                            .Where(p =>
                            {
                                var paidAt = p.PaidAt ?? p.CreatedAt;
                                return paidAt >= monthStart && paidAt < nextMonth;
                            })
                            .Sum(p => p.TotalAmount),
                        ReceivedEGP = completedPayments
                            .Where(p => p.PayoutStatus == PayoutStatus.Paid)
                            .Where(p =>
                            {
                                var receivedAt = p.PayoutCompletedAt ?? p.PaidAt ?? p.CreatedAt;
                                return receivedAt >= monthStart && receivedAt < nextMonth;
                            })
                            .Sum(p => p.VendorNetAmount)
                    };
                })
                .ToList();

            var activeContractCards = activeContracts
                .Where(c => c.Deadline >= now)
                .OrderBy(c => c.Deadline)
                .Take(5)
                .Select(c => new ActiveContractDTO
                {
                    SLAContractId = c.Id,
                    RequestId = c.RequestId,
                    ClientName = DecodeDisplayText(c.Request?.Client?.FullName),
                    RequestTitle = c.Request?.Title ?? string.Empty,
                    Deadline = c.Deadline,
                    ProgressPercent = c.Request?.RequestProgress?.Percentage ?? 0,
                    ContractStatus = c.SLAStatus.ToString()
                })
                .ToList();

            var upcomingDeadlines = activeContracts
                .Where(c => c.Deadline >= now)
                .OrderBy(c => c.Deadline)
                .Take(5)
                .Select(c => new UpcomingDeadlineDTO
                {
                    SLAContractId = c.Id,
                    RequestId = c.RequestId,
                    ClientName = DecodeDisplayText(c.Request?.Client?.FullName),
                    RequestTitle = c.Request?.Title ?? string.Empty,
                    Deadline = c.Deadline,
                    UrgencyLevel = GetUrgencyColor(c.Deadline, now)
                })
                .ToList();

            var proposalWinRate = new VendorProposalWinRateDTO
            {
                Submitted = submittedProposals,
                Accepted = acceptedProposals,
                Rejected = rejectedProposals,
                WinRatePercent = submittedProposals == 0
                    ? 0
                    : decimal.Round((acceptedProposals * 100m) / submittedProposals, 2)
            };

            return new VendorDashboardSummaryDTO
            {
                VendorQuickStats = vendorQuickStats,
                earningsOverTimes = earningsOverTimes,
                proposalWinRate = proposalWinRate,
                activeContracts = activeContractCards,
                upcomingDeadlines = upcomingDeadlines
            };
        }
        private static string BuildStatusDisplay(RequestStatus status, int proposalCount)
        {
            return status switch
            {
                RequestStatus.Active => "Active",
                RequestStatus.Completed => "Completed",
                RequestStatus.Pending when proposalCount > 0 => $"Proposals ({proposalCount})",
                _ => "Pending"
            };
        }

        private static string NormalizeInvoiceNumber(string? merchantOrderId, string requestId)
        {
            if (!string.IsNullOrWhiteSpace(merchantOrderId) && merchantOrderId.StartsWith("INV-", StringComparison.OrdinalIgnoreCase))
                return merchantOrderId;

            return $"INV-{requestId.Trim().ToUpperInvariant()}";
        }

        private static string GetUrgencyColor(DateTime deadline, DateTime now)
        {
            var remaining = deadline - now;

            if (remaining.TotalHours <= 24)
                return "Red";

            if (remaining.TotalHours <= 72)
                return "Orange";

            if (remaining.TotalDays <= 7)
                return "Green";

            return "Blue";
        }

        private static string DecodeDisplayText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Replace("+", " ");

            try
            {
                normalized = Uri.UnescapeDataString(normalized);
            }
            catch
            {
            }

            var htmlDecoded = WebUtility.HtmlDecode(normalized);
            var cleaned = new string(htmlDecoded
                .Where(ch => char.GetUnicodeCategory(ch) != UnicodeCategory.Format)
                .ToArray());

            return cleaned.Trim();
        }
    }
}
