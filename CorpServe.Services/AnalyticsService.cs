using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RatingModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AnalyticsDTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Result<AdminAnalyticsDashboardDTO>> GetAdminAnalyticsAsync(AnalyticsFilterDTO filter)
        {
            var normalizedRangeResult = NormalizeDateRange(filter);
            if (normalizedRangeResult.IsFailure)
                return normalizedRangeResult.Errors.ToList();

            var dateRange = normalizedRangeResult.Value;
            var previousStartDate = dateRange.StartDateUtc.AddDays(-dateRange.DaysCount);
            var previousEndDateExclusive = dateRange.StartDateUtc;
            var utcNow = DateTime.UtcNow;

            var requestsRepo = _unitOfWork.GetRepository<Request, string>();
            var proposalsRepo = _unitOfWork.GetRepository<Proposal, string>();
            var paymentsRepo = _unitOfWork.GetRepository<Payment, string>();
            var contractsRepo = _unitOfWork.GetRepository<SLAContract, string>();

            var completedPaymentsQuery = paymentsRepo.Query().Where(p => p.PaymentStatus == PaymentStatus.Completed);

            var currentGmv = await completedPaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var previousGmv = await completedPaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= previousStartDate && (p.PaidAt ?? p.CreatedAt) < previousEndDateExclusive)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var gmvTrend = dateRange.DaysCount <= 90
                ? await BuildDailyGmvTrendAsync(completedPaymentsQuery, dateRange)
                : await BuildMonthlyGmvTrendAsync(completedPaymentsQuery, dateRange);

            var categoryRows = await completedPaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .GroupBy(p => p.Request.Category != null ? p.Request.Category.Name : "Uncategorized")
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            var categoryTotal = categoryRows.Sum(x => x.Amount);
            var top4 = categoryRows.Take(4).ToList();
            var othersAmount = categoryRows.Skip(4).Sum(x => x.Amount);

            var topServiceCategories = top4
                .Select(x => new AdminServiceCategoryAnalyticsDTO
                {
                    CategoryName = x.CategoryName,
                    Percentage = categoryTotal == 0m ? 0m : decimal.Round((x.Amount * 100m) / categoryTotal, 2)
                })
                .ToList();

            if (othersAmount > 0m)
            {
                topServiceCategories.Add(new AdminServiceCategoryAnalyticsDTO
                {
                    CategoryName = "Others",
                    Percentage = categoryTotal == 0m ? 0m : decimal.Round((othersAmount * 100m) / categoryTotal, 2)
                });
            }

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var vendors = await _userManager.GetUsersInRoleAsync("Vendor");

            var monthStarts = GetMonthStarts(dateRange.StartDateUtc, dateRange.EndDateUtcExclusive).ToList();
            var userGrowth = monthStarts
                .Select(monthStart =>
                {
                    var nextMonth = monthStart.AddMonths(1);
                    return new AdminUserGrowthPointDTO
                    {
                        MonthUtc = monthStart,
                        Label = monthStart.ToString("MMM"),
                        ClientsCount = clients.Count(u => u.JoinedAt < nextMonth),
                        VendorsCount = vendors.Count(u => u.JoinedAt < nextMonth),
                        AdminsCount = admins.Count(u => u.JoinedAt < nextMonth)
                    };
                })
                .ToList();

            var activeWindowStart = dateRange.StartDateUtc;
            var activeWindowEnd = dateRange.EndDateUtcExclusive;

            var recentClientIdsFromRequests = await requestsRepo.Query()
                .Where(r => r.CreatedAt >= activeWindowStart && r.CreatedAt < activeWindowEnd)
                .Select(r => r.ClientId)
                .Distinct()
                .ToListAsync();

            var recentVendorIdsFromProposals = await proposalsRepo.Query()
                .Where(p => p.CreatedAt >= activeWindowStart && p.CreatedAt < activeWindowEnd)
                .Select(p => p.VendorId)
                .Distinct()
                .ToListAsync();

            var recentPaymentRows = await paymentsRepo.Query()
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= activeWindowStart && (p.PaidAt ?? p.CreatedAt) < activeWindowEnd)
                .Select(p => new { p.ClientId, p.VendorId })
                .ToListAsync();

            var activeUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in recentClientIdsFromRequests)
                activeUserIds.Add(id);
            foreach (var id in recentVendorIdsFromProposals)
                activeUserIds.Add(id);
            foreach (var row in recentPaymentRows)
            {
                activeUserIds.Add(row.ClientId);
                activeUserIds.Add(row.VendorId);
            }

            var allProposals = proposalsRepo.Query();
            var responseRows = await requestsRepo.Query()
                .Where(r => r.CreatedAt >= dateRange.StartDateUtc && r.CreatedAt < dateRange.EndDateUtcExclusive)
                .Select(r => new
                {
                    r.CreatedAt,
                    FirstResponseAt = allProposals
                        .Where(p => p.RequestId == r.Id)
                        .Min(p => (DateTime?)p.CreatedAt)
                })
                .Where(x => x.FirstResponseAt.HasValue)
                .ToListAsync();

            var avgTimeToMatchHours = responseRows.Count == 0
                ? 0m
                : decimal.Round((decimal)responseRows.Average(x => Math.Max(0d, (x.FirstResponseAt!.Value - x.CreatedAt).TotalHours)), 2);

            var slaRows = await contractsRepo.Query()
                .Where(c => c.Deadline >= dateRange.StartDateUtc && c.Deadline < dateRange.EndDateUtcExclusive)
                .Select(c => new
                {
                    c.SLAStatus,
                    c.Deadline,
                    CompletedAtUtc = c.Request.RequestProgress.UpdatedAt
                })
                .ToListAsync();

            var compliantCount = slaRows.Count(c =>
                (c.SLAStatus == SLAStatus.Completed && c.CompletedAtUtc <= c.Deadline)
                || (c.SLAStatus == SLAStatus.Inprogress && c.Deadline >= utcNow));

            var slaCompliancePercent = slaRows.Count == 0
                ? 0m
                : decimal.Round((compliantCount * 100m) / slaRows.Count, 2);

            var revenueRows = await completedPaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .Select(p => new { p.VendorNetAmount, p.Commision })
                .ToListAsync();

            var vendorPayout = revenueRows.Sum(x => x.VendorNetAmount);
            var platformFee = revenueRows.Sum(x => x.Commision);
            var splitTotal = vendorPayout + platformFee;

            var revenueSplit = new AdminRevenueSplitDTO
            {
                VendorPayoutEGP = vendorPayout,
                PlatformFeeEGP = platformFee,
                VendorPayoutPercent = splitTotal == 0m ? 0m : decimal.Round((vendorPayout * 100m) / splitTotal, 2),
                PlatformFeePercent = splitTotal == 0m ? 0m : decimal.Round((platformFee * 100m) / splitTotal, 2)
            };

            var topVendorRows = await completedPaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .GroupBy(p => p.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    RevenueEGP = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.RevenueEGP)
                .Take(5)
                .ToListAsync();

            var topVendorIds = topVendorRows.Select(x => x.VendorId).ToList();
            var vendorNameById = topVendorIds.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : await _userManager.Users
                    .AsNoTracking()
                    .Where(u => topVendorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName })
                    .ToDictionaryAsync(x => x.Id, x => x.FullName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var topVendorsByRevenue = topVendorRows
                .Select((x, index) => new AdminTopVendorRevenueDTO
                {
                    Rank = index + 1,
                    VendorId = x.VendorId,
                    VendorName = vendorNameById.GetValueOrDefault(x.VendorId, string.Empty),
                    RevenueEGP = x.RevenueEGP
                })
                .ToList();

            var anomalyRiskSection = await BuildAdminAnomalyRiskSectionAsync(paymentsRepo, contractsRepo, utcNow);

            return new AdminAnalyticsDashboardDTO
            {
                DateRange = new AnalyticsDateRangeDTO
                {
                    StartDateUtc = dateRange.StartDateUtc,
                    EndDateUtc = dateRange.EndDateUtcExclusive.AddDays(-1),
                    AppliedPreset = dateRange.AppliedPreset,
                    DaysCount = dateRange.DaysCount
                },
                Overview = new AdminAnalyticsOverviewDTO
                {
                    GmvEGP = currentGmv,
                    GmvChangePercent = previousGmv == 0m
                        ? (currentGmv > 0m ? 100m : 0m)
                        : decimal.Round(((currentGmv - previousGmv) / previousGmv) * 100m, 2),
                    ActiveUsersCount = activeUserIds.Count,
                    AvgTimeToMatchHours = avgTimeToMatchHours,
                    SlaCompliancePercent = slaCompliancePercent
                },
                PlatformGmvTrend = gmvTrend,
                TopServiceCategories = topServiceCategories,
                UserGrowth = userGrowth,
                RevenueSplit = revenueSplit,
                TopVendorsByRevenue = topVendorsByRevenue,
                AnomalyRiskFlags = anomalyRiskSection
            };
        }

        public async Task<Result<ClientAnaltyicsDashboardDTO>> GetClientAnalyticsAsync(string clientId, AnalyticsFilterDTO filter)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Validation("Analytics.ClientRequired", "Client identity is required.");

            var normalizedRangeResult = NormalizeDateRange(filter);
            if (normalizedRangeResult.IsFailure)
                return normalizedRangeResult.Errors.ToList();

            var dateRange = normalizedRangeResult.Value;
            var previousStartDate = dateRange.StartDateUtc.AddDays(-dateRange.DaysCount);
            var previousEndDateExclusive = dateRange.StartDateUtc;

            var requestsRepo = _unitOfWork.GetRepository<Request, string>();
            var proposalsRepo = _unitOfWork.GetRepository<Proposal, string>();
            var paymentsRepo = _unitOfWork.GetRepository<Payment, string>();
            var ratingsRepo = _unitOfWork.GetRepository<Rating, string>();

            var baseRequestsQuery = requestsRepo.Query().Where(r => r.ClientId == clientId);
            var basePaymentsQuery = paymentsRepo.Query()
                .Where(p => p.ClientId == clientId && p.PaymentStatus == PaymentStatus.Completed);

            var currentRequestsQuery = baseRequestsQuery
                .Where(r => r.CreatedAt >= dateRange.StartDateUtc && r.CreatedAt < dateRange.EndDateUtcExclusive);
            var previousRequestsQuery = baseRequestsQuery
                .Where(r => r.CreatedAt >= previousStartDate && r.CreatedAt < previousEndDateExclusive);

            var currentTotalRequests = await currentRequestsQuery.CountAsync();
            var previousTotalRequests = await previousRequestsQuery.CountAsync();

            var currentCompletedRequests = await currentRequestsQuery.CountAsync(r => r.RequestStatus == RequestStatus.Completed);
            var previousCompletedRequests = await previousRequestsQuery.CountAsync(r => r.RequestStatus == RequestStatus.Completed);

            var currentAvgResponseSeconds = await GetAverageResponseSecondsAsync(currentRequestsQuery, proposalsRepo.Query());
            var previousAvgResponseSeconds = await GetAverageResponseSecondsAsync(previousRequestsQuery, proposalsRepo.Query());

            var currentSpent = await basePaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var previousSpent = await basePaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= previousStartDate && (p.PaidAt ?? p.CreatedAt) < previousEndDateExclusive)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var spendingTrendRows = await basePaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .GroupBy(p => (p.PaidAt ?? p.CreatedAt).Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var trendByDate = spendingTrendRows.ToDictionary(x => x.Date, x => x.Amount);
            var spendingTrend = Enumerable.Range(0, dateRange.DaysCount)
                .Select(offset => dateRange.StartDateUtc.AddDays(offset))
                .Select(day => new AnalyticsTrendPointDTO
                {
                    DateUtc = day,
                    Label = day.ToString("dd MMM"),
                    AmountEGP = trendByDate.GetValueOrDefault(day, 0m)
                })
                .ToList();

            var categoryRows = await basePaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .GroupBy(p => p.Request.Category != null ? p.Request.Category.Name : "Uncategorized")
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            var categoryTotal = categoryRows.Sum(x => x.Amount);
            var spendingByCategory = categoryRows.Select(x => new AnalyticsCategorySpendingDTO
            {
                CategoryName = x.CategoryName,
                AmountEGP = x.Amount,
                Percentage = categoryTotal == 0m ? 0m : decimal.Round((x.Amount * 100m) / categoryTotal, 2)
            }).ToList();

            var statusRows = await currentRequestsQuery
                .GroupBy(r => r.RequestStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusCounts = statusRows.ToDictionary(x => x.Status, x => x.Count);
            var requestStatusBreakdown = Enum.GetValues<RequestStatus>()
                .Select(status =>
                {
                    var count = statusCounts.GetValueOrDefault(status, 0);
                    return new AnalyticsRequestStatusDTO
                    {
                        Status = status.ToString(),
                        Count = count,
                        Percentage = currentTotalRequests == 0 ? 0m : decimal.Round((count * 100m) / currentTotalRequests, 2)
                    };
                })
                .ToList();

            var allProposalsForTiming = proposalsRepo.Query();
            var responseTimeRows = await currentRequestsQuery
                .Select(r => new
                {
                    r.CreatedAt,
                    FirstResponseAt = allProposalsForTiming
                        .Where(p => p.RequestId == r.Id)
                        .Min(p => (DateTime?)p.CreatedAt)
                })
                .Where(x => x.FirstResponseAt.HasValue)
                .ToListAsync();

            var responseTimesInMinutes = responseTimeRows
                .Select(x => (int)Math.Max(0, (x.FirstResponseAt!.Value - x.CreatedAt).TotalMinutes))
                .ToList();

            var vendorResponseTime = BuildResponseTimeBuckets(responseTimesInMinutes);

            var topVendorRows = await basePaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .GroupBy(p => p.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    ServicesCount = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(5)
                .ToListAsync();

            var topVendorIds = topVendorRows.Select(x => x.VendorId).ToList();

            var vendorNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (topVendorIds.Count > 0)
            {
                vendorNameById = await _userManager.Users
                    .AsNoTracking()
                    .Where(u => topVendorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName })
                    .ToDictionaryAsync(x => x.Id, x => x.FullName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            }

            var ratingsByVendor = await ratingsRepo.Query()
                .Where(r => topVendorIds.Contains(r.VendorId))
                .GroupBy(r => r.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    AverageRating = g.Average(x => (decimal)x.Stars)
                })
                .ToDictionaryAsync(x => x.VendorId, x => decimal.Round(x.AverageRating, 2), StringComparer.OrdinalIgnoreCase);

            var topVendorsUsed = topVendorRows
                .Select((x, index) => new AnalyticsTopVendorDTO
                {
                    Rank = index + 1,
                    VendorId = x.VendorId,
                    VendorName = vendorNameById.GetValueOrDefault(x.VendorId, string.Empty),
                    ServicesCount = x.ServicesCount,
                    AverageRating = ratingsByVendor.GetValueOrDefault(x.VendorId, 0m),
                    TotalSpentEGP = x.TotalSpent
                })
                .ToList();

            var overview = new AnalyticsOverviewDTO
            {
                TotalRequests = currentTotalRequests,
                TotalRequestsChange = currentTotalRequests - previousTotalRequests,
                AvgResponseTimeDays = decimal.Round((decimal)(currentAvgResponseSeconds / 86400d), 4),
                AvgResponseTimeChangeDays = decimal.Round((decimal)((currentAvgResponseSeconds - previousAvgResponseSeconds) / 86400d), 4),
                TotalSpentEGP = currentSpent,
                TotalSpentChangePercent = previousSpent == 0m
                    ? (currentSpent > 0m ? 100m : 0m)
                    : decimal.Round(((currentSpent - previousSpent) / previousSpent) * 100m, 2),
                ServiceSuccessRatePercent = currentTotalRequests == 0
                    ? 0m
                    : decimal.Round((currentCompletedRequests * 100m) / currentTotalRequests, 2),
                ServiceSuccessRateChangePercent =
                    (currentTotalRequests == 0
                        ? 0m
                        : decimal.Round((currentCompletedRequests * 100m) / currentTotalRequests, 2))
                    -
                    (previousTotalRequests == 0
                        ? 0m
                        : decimal.Round((previousCompletedRequests * 100m) / previousTotalRequests, 2))
            };

            return new ClientAnaltyicsDashboardDTO
            {
                DateRange = new AnalyticsDateRangeDTO
                {
                    StartDateUtc = dateRange.StartDateUtc,
                    EndDateUtc = dateRange.EndDateUtcExclusive.AddDays(-1),
                    AppliedPreset = dateRange.AppliedPreset,
                    DaysCount = dateRange.DaysCount
                },
                Overview = overview,
                SpendingTrend = spendingTrend,
                SpendingByCategory = spendingByCategory,
                RequestStatusBreakdown = requestStatusBreakdown,
                VendorResponseTime = vendorResponseTime,
                TopVendorsUsed = topVendorsUsed
            };
        }
        public async Task<Result<VendorAnalyticsDashboardDTO>> GetVendorAnalyticsAsync(string vendorId, AnalyticsFilterDTO filter)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Error.Validation("Analytics.VendorRequired", "Vendor identity is required.");

            var normalizedRangeResult = NormalizeDateRange(filter);
            if (normalizedRangeResult.IsFailure)
                return normalizedRangeResult.Errors.ToList();

            var dateRange = normalizedRangeResult.Value;
            var previousStartDate = dateRange.StartDateUtc.AddDays(-dateRange.DaysCount);
            var previousEndDateExclusive = dateRange.StartDateUtc;

            var proposalsRepo = _unitOfWork.GetRepository<Proposal, string>();
            var contractsRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var ratingsRepo = _unitOfWork.GetRepository<Rating, string>();

            var baseProposalsQuery = proposalsRepo.Query().Where(p => p.VendorId == vendorId);

            var currentProposalsSent = await baseProposalsQuery
                .Where(p => p.CreatedAt >= dateRange.StartDateUtc && p.CreatedAt < dateRange.EndDateUtcExclusive)
                .CountAsync();

            var previousProposalsSent = await baseProposalsQuery
                .Where(p => p.CreatedAt >= previousStartDate && p.CreatedAt < previousEndDateExclusive)
                .CountAsync();

            var currentAcceptedProposals = await baseProposalsQuery
                .Where(p => p.CreatedAt >= dateRange.StartDateUtc && p.CreatedAt < dateRange.EndDateUtcExclusive)
                .CountAsync(p => p.ProposalStatus == ClientStatus.Accepted);

            var previousAcceptedProposals = await baseProposalsQuery
                .Where(p => p.CreatedAt >= previousStartDate && p.CreatedAt < previousEndDateExclusive)
                .CountAsync(p => p.ProposalStatus == ClientStatus.Accepted);

            var currentWinRate = currentProposalsSent == 0
                ? 0m
                : decimal.Round((currentAcceptedProposals * 100m) / currentProposalsSent, 2);

            var previousWinRate = previousProposalsSent == 0
                ? 0m
                : decimal.Round((previousAcceptedProposals * 100m) / previousProposalsSent, 2);

            var currentAvgContractValue = await contractsRepo.Query()
                .Where(c => c.VendorId == vendorId
                    && c.CreatedAt >= dateRange.StartDateUtc
                    && c.CreatedAt < dateRange.EndDateUtcExclusive)
                .AverageAsync(c => (decimal?)c.ContractPrice) ?? 0m;

            var completedContractsInRange = await contractsRepo.Query()
                .Where(c => c.VendorId == vendorId && c.SLAStatus == SLAStatus.Completed)
                .Select(c => new
                {
                    DeliveredAtUtc = c.Request.RequestProgress.UpdatedAt,
                    c.Deadline
                })
                .Where(x => x.DeliveredAtUtc >= dateRange.StartDateUtc && x.DeliveredAtUtc < dateRange.EndDateUtcExclusive)
                .ToListAsync();

            var onTimeDeliveredCount = completedContractsInRange.Count(c => c.DeliveredAtUtc <= c.Deadline);
            var onTimeDeliveryPercent = completedContractsInRange.Count == 0
                ? 0m
                : decimal.Round((onTimeDeliveredCount * 100m) / completedContractsInRange.Count, 2);

            var currentAvgRating = await ratingsRepo.Query()
                .Where(r => r.VendorId == vendorId
                    && r.CreatedAt >= dateRange.StartDateUtc
                    && r.CreatedAt < dateRange.EndDateUtcExclusive)
                .AverageAsync(r => (decimal?)r.Stars) ?? 0m;

            var acceptedTrendRows = await baseProposalsQuery
                .Where(p => p.ProposalStatus == ClientStatus.Accepted
                    && p.CreatedAt >= dateRange.StartDateUtc
                    && p.CreatedAt < dateRange.EndDateUtcExclusive)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var acceptedTrendByDate = acceptedTrendRows.ToDictionary(x => x.Date, x => x.Count);
            var acceptedProposalsTrend = Enumerable.Range(0, dateRange.DaysCount)
                .Select(offset => dateRange.StartDateUtc.AddDays(offset))
                .Select(day => new VendorAcceptedProposalTrendPointDTO
                {
                    DateUtc = day,
                    Label = day.ToString("dd MMM"),
                    AcceptedCount = acceptedTrendByDate.GetValueOrDefault(day, 0)
                })
                .ToList();

            var retentionRows = await contractsRepo.Query()
                .Where(c => c.VendorId == vendorId && c.SLAStatus == SLAStatus.Completed)
                .Select(c => new
                {
                    c.ClientId,
                    DeliveredAtUtc = c.Request.RequestProgress.UpdatedAt
                })
                .Where(x => x.DeliveredAtUtc >= dateRange.StartDateUtc && x.DeliveredAtUtc < dateRange.EndDateUtcExclusive)
                .ToListAsync();

            var servedClientGroups = retentionRows
                .GroupBy(x => x.ClientId)
                .ToList();

            var totalClientsServed = servedClientGroups.Count;
            var repeatClientsCount = servedClientGroups.Count(g => g.Count() >= 2);

            var ratingDistributionRows = await ratingsRepo.Query()
                .Where(r => r.VendorId == vendorId
                    && r.CreatedAt >= dateRange.StartDateUtc
                    && r.CreatedAt < dateRange.EndDateUtcExclusive)
                .GroupBy(r => r.Stars)
                .Select(g => new { Stars = g.Key, Count = g.Count() })
                .ToListAsync();

            var ratingDistributionByStars = ratingDistributionRows.ToDictionary(x => x.Stars, x => x.Count);
            var ratingsDistribution = new VendorRatingsDistributionDTO
            {
                AverageRating = decimal.Round(currentAvgRating, 2),
                TotalRatingsCount = ratingDistributionRows.Sum(x => x.Count),
                StarsBreakdown = Enumerable.Range(1, 5)
                    .OrderByDescending(x => x)
                    .Select(stars => new VendorRatingStarCountDTO
                    {
                        Stars = stars,
                        Count = ratingDistributionByStars.GetValueOrDefault(stars, 0)
                    })
                    .ToList()
            };

            var topContractRows = await contractsRepo.Query()
                .Where(c => c.VendorId == vendorId && c.SLAStatus == SLAStatus.Completed)
                .Select(c => new
                {
                    c.ClientId,
                    ClientName = c.Client.FullName,
                    Service = c.Request.Category != null ? c.Request.Category.Name : "Uncategorized",
                    ValueEGP = c.ContractPrice,
                    DeliveredAtUtc = c.Request.RequestProgress.UpdatedAt,
                    c.Deadline,
                    Rating = c.Request.Rating != null ? (decimal?)c.Request.Rating.Stars : null
                })
                .Where(x => x.DeliveredAtUtc >= dateRange.StartDateUtc && x.DeliveredAtUtc < dateRange.EndDateUtcExclusive)
                .OrderByDescending(x => x.ValueEGP)
                .ThenByDescending(x => x.DeliveredAtUtc)
                .Take(5)
                .ToListAsync();

            var topPerformingContracts = topContractRows
                .Select(x =>
                {
                    var deliveryDeltaDays = (x.Deadline.Date - x.DeliveredAtUtc.Date).Days;
                    var deliveryStatus = deliveryDeltaDays switch
                    {
                        > 0 => $"+{deliveryDeltaDays} ahead",
                        < 0 => $"{deliveryDeltaDays} late",
                        _ => "On time"
                    };

                    return new VendorTopPerformingContractDTO
                    {
                        ClientId = x.ClientId,
                        ClientName = string.IsNullOrWhiteSpace(x.ClientName) ? string.Empty : x.ClientName,
                        ClientProfileUrl = $"/vendor/user/{x.ClientId}",
                        Service = x.Service,
                        ValueEGP = x.ValueEGP,
                        DeliveredAtUtc = x.DeliveredAtUtc,
                        DeliveryDeltaDays = deliveryDeltaDays,
                        DeliveryStatus = deliveryStatus,
                        Rating = decimal.Round(x.Rating ?? 0m, 1)
                    };
                })
                .ToList();

            return new VendorAnalyticsDashboardDTO
            {
                DateRange = new AnalyticsDateRangeDTO
                {
                    StartDateUtc = dateRange.StartDateUtc,
                    EndDateUtc = dateRange.EndDateUtcExclusive.AddDays(-1),
                    AppliedPreset = dateRange.AppliedPreset,
                    DaysCount = dateRange.DaysCount
                },
                Overview = new VendorAnalyticsOverviewDTO
                {
                    ProposalsSent = currentProposalsSent,
                    WinRatePercent = currentWinRate,
                    WinRateChangePercent = decimal.Round(currentWinRate - previousWinRate, 2),
                    AvgContractValueEGP = decimal.Round(currentAvgContractValue, 2),
                    OnTimeDeliveryPercent = onTimeDeliveryPercent,
                    AvgRating = decimal.Round(currentAvgRating, 2)
                },
                AcceptedProposalsTrend = acceptedProposalsTrend,
                ClientRetention = new VendorClientRetentionDTO
                {
                    TotalClientsServed = totalClientsServed,
                    RepeatClientsCount = repeatClientsCount,
                    RepeatClientsPercent = totalClientsServed == 0
                        ? 0m
                        : decimal.Round((repeatClientsCount * 100m) / totalClientsServed, 2)
                },
                RatingsDistribution = ratingsDistribution,
                TopPerformingContracts = topPerformingContracts
            };
        }

        #region Helper Methods
        private static List<AnalyticsResponseTimeBucketDTO> BuildResponseTimeBuckets(IEnumerable<int> responseTimesInMinutes)
        {
            var buckets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["<1hr"] = 0,
                ["1-4hr"] = 0,
                ["4-12hr"] = 0,
                ["12-24hr"] = 0,
                [">24hr"] = 0
            };

            foreach (var minutes in responseTimesInMinutes)
            {
                if (minutes < 60)
                    buckets["<1hr"]++;
                else if (minutes < 240)
                    buckets["1-4hr"]++;
                else if (minutes < 720)
                    buckets["4-12hr"]++;
                else if (minutes < 1440)
                    buckets["12-24hr"]++;
                else
                    buckets[">24hr"]++;
            }

            return buckets.Select(x => new AnalyticsResponseTimeBucketDTO
            {
                Bucket = x.Key,
                Count = x.Value
            }).ToList();
        }
        private static async Task<double> GetAverageResponseSecondsAsync(
            IQueryable<Request> requestsQuery,
            IQueryable<Proposal> proposalsQuery)
        {
            var responseRows = await requestsQuery
                .Select(r => new
                {
                    r.CreatedAt,
                    FirstResponseAt = proposalsQuery
                        .Where(p => p.RequestId == r.Id)
                        .Min(p => (DateTime?)p.CreatedAt)
                })
                .Where(x => x.FirstResponseAt.HasValue)
                .ToListAsync();

            if (responseRows.Count == 0)
                return 0d;

            return responseRows.Average(x => Math.Max(0d, (x.FirstResponseAt!.Value - x.CreatedAt).TotalSeconds));
        }

        private static async Task<List<AdminGmvTrendPointDTO>> BuildDailyGmvTrendAsync(
            IQueryable<Payment> completedPaymentsQuery,
            NormalizedDateRange dateRange)
        {
            var rows = await completedPaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .GroupBy(p => (p.PaidAt ?? p.CreatedAt).Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var byDate = rows.ToDictionary(x => x.Date, x => x.Amount);

            return Enumerable.Range(0, dateRange.DaysCount)
                .Select(offset => dateRange.StartDateUtc.AddDays(offset))
                .Select(day => new AdminGmvTrendPointDTO
                {
                    DateUtc = day,
                    Label = day.ToString("dd MMM"),
                    GmvEGP = byDate.GetValueOrDefault(day, 0m)
                })
                .ToList();
        }

        private static async Task<List<AdminGmvTrendPointDTO>> BuildMonthlyGmvTrendAsync(
            IQueryable<Payment> completedPaymentsQuery,
            NormalizedDateRange dateRange)
        {
            var monthStarts = GetMonthStarts(dateRange.StartDateUtc, dateRange.EndDateUtcExclusive).ToList();

            var rows = await completedPaymentsQuery
                .Where(p => (p.PaidAt ?? p.CreatedAt) >= dateRange.StartDateUtc && (p.PaidAt ?? p.CreatedAt) < dateRange.EndDateUtcExclusive)
                .GroupBy(p => new { Year = (p.PaidAt ?? p.CreatedAt).Year, Month = (p.PaidAt ?? p.CreatedAt).Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var byMonth = rows.ToDictionary(
                x => new DateTime(x.Year, x.Month, 1),
                x => x.Amount);

            return monthStarts
                .Select(month => new AdminGmvTrendPointDTO
                {
                    DateUtc = month,
                    Label = month.ToString("MMM"),
                    GmvEGP = byMonth.GetValueOrDefault(month, 0m)
                })
                .ToList();
        }

        private static IEnumerable<DateTime> GetMonthStarts(DateTime startDateUtc, DateTime endDateUtcExclusive)
        {
            var cursor = new DateTime(startDateUtc.Year, startDateUtc.Month, 1);
            var endDate = endDateUtcExclusive.AddDays(-1);
            var endMonthStart = new DateTime(endDate.Year, endDate.Month, 1);

            while (cursor <= endMonthStart)
            {
                yield return cursor;
                cursor = cursor.AddMonths(1);
            }
        }

        private static async Task<AdminAnomalyRiskSectionDTO> BuildAdminAnomalyRiskSectionAsync(
            IGenericRepository<Payment, string> paymentsRepo,
            IGenericRepository<SLAContract, string> contractsRepo,
            DateTime utcNow)
        {
            var flags = new List<AdminAnomalyRiskFlagDTO>();

            var payoutOverdueRows = await paymentsRepo.Query()
                .Where(p => p.PaymentStatus == PaymentStatus.Completed
                    && p.PayoutStatus != PayoutStatus.Paid
                    && (p.PaidAt ?? p.CreatedAt) <= utcNow.AddDays(-7))
                .Select(p => new
                {
                    p.MerchantOrderId,
                    p.RequestId,
                    BaseDate = p.PaidAt ?? p.CreatedAt
                })
                .ToListAsync();

            foreach (var row in payoutOverdueRows)
            {
                var overdueDays = Math.Max(1, (utcNow.Date - row.BaseDate.Date).Days);
                var severity = overdueDays >= 14 ? "High" : "Medium";
                flags.Add(new AdminAnomalyRiskFlagDTO
                {
                    Type = "Payment Overdue",
                    Entity = $"Invoice {NormalizeInvoiceNumber(row.MerchantOrderId, row.RequestId)}",
                    Description = $"Vendor payout {overdueDays} days overdue.",
                    Severity = severity,
                    DetectedAtUtc = row.BaseDate
                });
            }

            var pendingPaymentRows = await paymentsRepo.Query()
                .Where(p => p.PaymentStatus == PaymentStatus.Pending
                    && p.CreatedAt <= utcNow.AddDays(-15))
                .Select(p => new
                {
                    p.MerchantOrderId,
                    p.RequestId,
                    p.CreatedAt
                })
                .ToListAsync();

            foreach (var row in pendingPaymentRows)
            {
                var overdueDays = Math.Max(1, (utcNow.Date - row.CreatedAt.Date).Days);
                flags.Add(new AdminAnomalyRiskFlagDTO
                {
                    Type = "Payment Overdue",
                    Entity = $"Invoice {NormalizeInvoiceNumber(row.MerchantOrderId, row.RequestId)}",
                    Description = $"Client payment pending for {overdueDays} days.",
                    Severity = "High",
                    DetectedAtUtc = row.CreatedAt
                });
            }

            var breachRiskRows = await contractsRepo.Query()
                .Where(c => c.SLAStatus != SLAStatus.Completed
                    && c.Deadline <= utcNow.AddDays(7))
                .Select(c => new
                {
                    c.Id,
                    c.Deadline,
                    ProgressPercent = c.Request.RequestProgress.Percentage,
                    ProgressUpdatedAt = c.Request.RequestProgress.UpdatedAt
                })
                .ToListAsync();

            foreach (var row in breachRiskRows)
            {
                var deliveryGapDays = (row.Deadline.Date - utcNow.Date).Days;
                var description = deliveryGapDays >= 0
                    ? $"{deliveryGapDays} days to deadline, {row.ProgressPercent}% progress."
                    : $"Deadline passed by {Math.Abs(deliveryGapDays)} days, {row.ProgressPercent}% progress.";

                var severity = deliveryGapDays < 0 || (deliveryGapDays <= 3 && row.ProgressPercent < 70)
                    ? "High"
                    : "Medium";

                flags.Add(new AdminAnomalyRiskFlagDTO
                {
                    Type = "SLA Breach Risk",
                    Entity = $"Contract {row.Id}",
                    Description = description,
                    Severity = severity,
                    DetectedAtUtc = row.ProgressUpdatedAt == default ? utcNow : row.ProgressUpdatedAt
                });
            }

            var orderedFlags = flags
                .OrderByDescending(f => SeverityRank(f.Severity))
                .ThenByDescending(f => f.DetectedAtUtc)
                .Take(20)
                .ToList();

            return new AdminAnomalyRiskSectionDTO
            {
                ActiveFlagsCount = orderedFlags.Count,
                Flags = orderedFlags
            };
        }

        private static int SeverityRank(string severity) => severity switch
        {
            "High" => 3,
            "Medium" => 2,
            _ => 1
        };

        private static string NormalizeInvoiceNumber(string? merchantOrderId, string requestId)
        {
            if (!string.IsNullOrWhiteSpace(merchantOrderId) && merchantOrderId.StartsWith("INV-", StringComparison.OrdinalIgnoreCase))
                return merchantOrderId;

            if (!string.IsNullOrWhiteSpace(merchantOrderId))
                return merchantOrderId;

            return $"INV-{requestId.Trim().ToUpperInvariant()}";
        }

        private static Result<NormalizedDateRange> NormalizeDateRange(AnalyticsFilterDTO? filter)
        {
            var utcToday = DateTime.UtcNow.Date;
            var preset = filter?.RangePreset ?? AnalyticsDateRangePreset.Last30Days;

            if (preset == AnalyticsDateRangePreset.Custom)
            {
                if (filter?.StartDateUtc is null || filter.EndDateUtc is null)
                    return Error.Validation("Analytics.CustomRangeRequired", "StartDateUtc and EndDateUtc are required for custom range.");

                var customStart = filter.StartDateUtc.Value.Date;
                var customEnd = filter.EndDateUtc.Value.Date;

                if (customStart > customEnd)
                    return Error.Validation("Analytics.InvalidRange", "StartDateUtc must be earlier than or equal to EndDateUtc.");

                var customDays = (customEnd - customStart).Days + 1;
                if (customDays > 366)
                    return Error.Validation("Analytics.RangeTooLarge", "Custom range must not exceed 366 days.");

                return new NormalizedDateRange
                {
                    StartDateUtc = customStart,
                    EndDateUtcExclusive = customEnd.AddDays(1),
                    AppliedPreset = AnalyticsDateRangePreset.Custom.ToString(),
                    DaysCount = customDays
                };
            }

            var days = preset switch
            {
                AnalyticsDateRangePreset.Last7Days => 7,
                AnalyticsDateRangePreset.Last30Days => 30,
                AnalyticsDateRangePreset.Last90Days => 90,
                _ => 30
            };

            var startDate = utcToday.AddDays(-(days - 1));
            return new NormalizedDateRange
            {
                StartDateUtc = startDate,
                EndDateUtcExclusive = utcToday.AddDays(1),
                AppliedPreset = preset.ToString(),
                DaysCount = days
            };
        }
        private sealed class NormalizedDateRange
        {
            public DateTime StartDateUtc { get; set; }
            public DateTime EndDateUtcExclusive { get; set; }
            public string AppliedPreset { get; set; } = default!;
            public int DaysCount { get; set; }
        } 
        #endregion
    }
}
