using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.RatingModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.DashboardDTOs.ClientDTOs;
using CorpServe.Shared.DTOs.DashboardDTOs.VendorDTOs;
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

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            var requests = (await requestRepo.GetAllAsync(new ClientDashboardRequestsSpecification(clientId))).ToList();
            var pendingProposals = (await proposalRepo.GetAllAsync(new ClientPendingProposalsForDashboardSpecification(clientId))).ToList();
            var completedPayments = (await paymentRepo.GetAllAsync(new ClientCompletedPaymentsForDashboardSpecification(clientId))).ToList();
            var pendingPayments = (await paymentRepo.GetAllAsync(new ClientPendingPaymentsForDashboardSpecification(clientId))).ToList();

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
                .Range(0, 7)
                .Select(index => startOfToday.AddDays(index - 6))
                .Select(day =>
                {
                    var nextDay = day.AddDays(1);
                    return new RequestActivityDTO
                    {
                        DayLabel = day.ToString("ddd"),
                        Date = day,
                        Created = requests.Count(r => r.CreatedAt >= day && r.CreatedAt < nextDay),
                        Completed = requests.Count(r =>
                            r.RequestStatus == RequestStatus.Completed
                            && r.CreatedAt >= day
                            && r.CreatedAt < nextDay)
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

            var contracts = (await contractRepo.GetAllAsync(new VendorDashboardContractsSpecification(vendorId))).ToList();
            var proposals = (await proposalRepo.GetAllAsync(new VendorDashboardProposalsSpecification(vendorId))).ToList();
            var payments = (await paymentRepo.GetAllAsync(new VendorDashboardPaymentsSpecification(vendorId))).ToList();
            var ratings = (await ratingRepo.GetAllAsync(new VendorDashboardRatingsSpecification(vendorId))).ToList();

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
                RevenueThisMonthEGP = (int)Math.Round(currentMonthRevenue),
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
