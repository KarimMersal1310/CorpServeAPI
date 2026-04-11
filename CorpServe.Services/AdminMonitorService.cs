using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AdminDTOs;
using CorpServe.Shared.QueryParams;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Services
{
    public class AdminMonitorService : IAdminMonitorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminMonitorService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<AdminUsersManageDTO> GetUsersForManagementAsync(AdminUserManagementQueryParams queryParams)
        {
            var normalizedRole = queryParams.Role?.Trim().ToLowerInvariant();

            var includeClient = true;
            var includeVendor = true;

            var users = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);
            var userRoles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (includeClient)
            {
                var clients = await _userManager.GetUsersInRoleAsync("Client");
                foreach (var user in clients)
                {
                    users[user.Id] = user;
                    userRoles[user.Id] = "Client";
                }
            }

            if (includeVendor)
            {
                var vendors = await _userManager.GetUsersInRoleAsync("Vendor");
                foreach (var user in vendors)
                {
                    users[user.Id] = user;
                    userRoles[user.Id] = "Vendor";
                }
            }

            var allUsers = users.Values.ToList();

            var totalUsers = allUsers.Count;
            var activeCount = allUsers.Count(u => u.Status == UserStatus.Active);
            var suspendedCount = allUsers.Count(u => u.Status == UserStatus.Suspended);
            var clientsCount = allUsers.Count(u => string.Equals(userRoles.GetValueOrDefault(u.Id, string.Empty), "Client", StringComparison.OrdinalIgnoreCase));
            var vendorsCount = allUsers.Count(u => string.Equals(userRoles.GetValueOrDefault(u.Id, string.Empty), "Vendor", StringComparison.OrdinalIgnoreCase));

            IEnumerable<ApplicationUser> filteredUsers = allUsers;

            if (normalizedRole is "client" or "vendor")
            {
                var targetRole = normalizedRole == "client" ? "Client" : "Vendor";
                filteredUsers = filteredUsers.Where(u => string.Equals(userRoles.GetValueOrDefault(u.Id, string.Empty), targetRole, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search.Trim();
                filteredUsers = filteredUsers.Where(u =>
                    (!string.IsNullOrWhiteSpace(u.FullName) && u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(u.Email) && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var sortedUsers = filteredUsers
                .OrderBy(u => u.FullName)
                .ToList();

            var count = sortedUsers.Count;
            var pageUsers = sortedUsers
                .Skip((queryParams.PageIndex - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();

            var pageUserIds = pageUsers.Select(u => u.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var requestCountsByClientId = await requestRepo
                .GetAllAsync(new RequestsForClientsSpecification(pageUserIds));

            var requestCountLookup = requestCountsByClientId
                .GroupBy(r => r.ClientId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var handledCountsByVendorId = await slaRepo
                .GetAllAsync(new SlaContractsForVendorsSpecification(pageUserIds));

            var handledCountLookup = handledCountsByVendorId
                .GroupBy(s => s.VendorId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var data = new List<AdminUserManagementDTO>();

            foreach (var user in pageUsers)
            {
                var role = userRoles.GetValueOrDefault(user.Id, string.Empty);
                var requestsCreatedCount = role == "Client"
                    ? requestCountLookup.GetValueOrDefault(user.Id, 0)
                    : 0;

                var requestsHandledCount = role == "Vendor"
                    ? handledCountLookup.GetValueOrDefault(user.Id, 0)
                    : 0;

                data.Add(new AdminUserManagementDTO
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = role,
                    Joined = user.JoinedAt,
                    Status = user.Status.ToString(),
                    RequestsCreatedCount = requestsCreatedCount,
                    RequestsHandledCount = requestsHandledCount
                });
            }

            return new AdminUsersManageDTO
            {
                Summary = new AdminUsersSummaryDTO
                {
                    TotalUsers = totalUsers,
                    ActiveCount = activeCount,
                    SuspendedCount = suspendedCount,
                    ClientsCount = clientsCount,
                    VendorsCount = vendorsCount
                },
                Users = new PaginatedResult<AdminUserManagementDTO>(queryParams.PageIndex, queryParams.PageSize, count, data)
            };
        }

        public async Task<Result<bool>> SuspendUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Validation("User.IdRequired", "User ID is required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            user.Status = UserStatus.Suspended;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return Error.Failure("User.SuspendFailed", string.Join(", ", result.Errors.Select(e => e.Description)));

            return true;
        }

        public async Task<Result<bool>> ActivateUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Validation("User.IdRequired", "User ID is required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            user.Status = UserStatus.Active;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return Error.Failure("User.ActivationFailed", string.Join(", ", result.Errors.Select(e => e.Description)));

            return true;
        }

        public async Task<AdminRequestsManageDTO> GetRequestMonitorAsync(AdminRequestMonitorQueryParams queryParams)
        {
            var requestRepo = _unitOfWork.GetRepository<Request, string>();

            ResolveSlaDisplayFilterFlags(
                queryParams.SlaDisplayFilter,
                out var useSlaDisplayFilter,
                out var slaNa,
                out var slaActive,
                out var slaAtRisk,
                out var slaDelayed,
                out var slaCompleted);

            var listSpecification = new AdminRequestMonitorListSpecification(
                queryParams.Search,
                queryParams.CategoryId,
                queryParams.RequestStatus,
                queryParams.SlaStatus,
                useSlaDisplayFilter,
                slaNa,
                slaActive,
                slaAtRisk,
                slaDelayed,
                slaCompleted,
                queryParams.PageSize,
                queryParams.PageIndex);

            var countSpecification = new AdminRequestMonitorCountSpecification(
                queryParams.Search,
                queryParams.CategoryId,
                queryParams.RequestStatus,
                queryParams.SlaStatus,
                useSlaDisplayFilter,
                slaNa,
                slaActive,
                slaAtRisk,
                slaDelayed,
                slaCompleted);

            var requests = (await requestRepo.GetAllAsync(listSpecification)).ToList();
            var count = await requestRepo.CountAsync(countSpecification);
            var aggregateRequests = (await requestRepo.GetAllAsync(new AdminRequestMonitorAggregateSpecification(
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                false,
                false,
                false))).ToList();

            var vendorIds = requests
                .SelectMany(r => r.Proposals.Select(p => p.VendorId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var vendorNamesById = vendorIds.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : await _userManager.Users
                    .Where(u => vendorIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName, StringComparer.OrdinalIgnoreCase);

            var data = requests.Select(r =>
            {
                var selectedProposal = r.Proposals.FirstOrDefault(p => p.IsSelected);

                return new AdminRequestMonitorDTO
                {
                    RequestId = r.Id,
                    Title = r.Title,
                    Description = r.Discription,
                    ClientName = r.Client?.FullName ?? string.Empty,
                    VendorName = r.SLAContract?.Vendor?.FullName ?? selectedProposal?.Vendor?.FullName,
                    CategoryName = r.Category?.Name ?? string.Empty,
                    BudgetMin = r.BudgetMin,
                    BudgetMax = r.BudgetMax,
                    Deadline = r.SLAContract?.Deadline ?? selectedProposal?.ProposedDeadline,
                    Progress = r.RequestProgress?.Percentage ?? 0,
                    RequestStatus = r.RequestStatus.ToString(),
                    SlaStatus = r.SLAContract is null ? null : r.SLAContract.SLAStatus.ToString(),
                    NumberOfProposals = r.Proposals.Count,
                    Proposals = r.Proposals
                        .OrderByDescending(p => p.CreatedAt)
                        .Select(p => new AdminRequestProposalDTO
                        {
                            ProposalId = p.Id,
                            VendorId = p.VendorId,
                            VendorName = vendorNamesById.GetValueOrDefault(p.VendorId, string.Empty),
                            ProposalStatus = p.ProposalStatus.ToString(),
                            ProposalType = p.ProposalType.ToString(),
                            ProposedPrice = p.ProposedPrice,
                            ProposedDeadline = p.ProposedDeadline,
                            CreatedAt = p.CreatedAt,
                            Message = p.Message
                        })
                        .ToList()
                };
            }).ToList();

            var totalBudgetMin = aggregateRequests.Sum(r => r.BudgetMin);
            var totalBudgetMax = aggregateRequests.Sum(r => r.BudgetMax);
            var activeCount = aggregateRequests.Count(r => r.RequestStatus == RequestStatus.Active);
            var pendingCount = aggregateRequests.Count(r => r.RequestStatus == RequestStatus.Pending);
            var delayedSlaCount = aggregateRequests.Count(r => r.SLAContract != null && r.SLAContract.SLAStatus == SLAStatus.Delayed);
            var avgProgress = aggregateRequests.Count == 0
                ? 0
                : (int)Math.Round(aggregateRequests.Average(r => (double)(r.RequestProgress?.Percentage ?? 0)));

            return new AdminRequestsManageDTO
            {
                Summary = new AdminRequestsSummaryDTO
                {
                    TotalRequests = aggregateRequests.Count,
                    ActiveCount = activeCount,
                    PendingCount = pendingCount,
                    DelayedSlaCount = delayedSlaCount,
                    AvgProgress = avgProgress,
                    TotalBudgetMin = totalBudgetMin,
                    TotalBudgetMax = totalBudgetMax
                },
                Requests = new PaginatedResult<AdminRequestMonitorDTO>(queryParams.PageIndex, queryParams.PageSize, count, data)
            };
        }

        public async Task<Result<AdminSlaMonitorDTO>> GetSlaMonitorAsync(AdminSlaMonitorQueryParams queryParams)
        {
            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();

            var statusFilter = queryParams.ContractStatus ?? queryParams.SlaStatus;

            var listSpecification = new AdminSlaMonitorListSpecification(
                queryParams.Search,
                statusFilter,
                queryParams.CategoryId,
                queryParams.PageSize,
                queryParams.PageIndex);

            var countSpecification = new AdminSlaMonitorCountSpecification(
                queryParams.Search,
                statusFilter,
                queryParams.CategoryId);

            var contracts = (await slaRepo.GetAllAsync(listSpecification)).ToList();
            var count = await slaRepo.CountAsync(countSpecification);

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var requestIds = contracts.Select(c => c.RequestId).Distinct().ToList();
            var categoryNameByRequestId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (requestIds.Count > 0)
            {
                var reqsWithCategory = await requestRepo.GetAllAsync(new RequestsByIdsWithCategorySpecification(requestIds));
                foreach (var r in reqsWithCategory)
                    categoryNameByRequestId[r.Id] = r.Category?.Name ?? string.Empty;
            }

            var utcNow = DateTime.UtcNow;
            var data = contracts.Select(c =>
            {
                var warning = ResolveWarningLevel(c);
                return new AdminSlaContractMonitorDTO
                {
                    SlaContractId = c.Id,
                    RequestId = c.RequestId,
                    RequestTitle = c.Request?.Title ?? string.Empty,
                    ClientName = c.Client?.FullName ?? string.Empty,
                    VendorName = c.Vendor?.FullName ?? string.Empty,
                    Price = c.ContractPrice,
                    CreatedAt = c.CreatedAt,
                    Deadline = c.Deadline,
                    SlaStatus = c.SLAStatus.ToString(),
                    WarningLevel = warning,
                    WarningLevelUi = MapWarningLevelUi(warning),
                    CategoryName = categoryNameByRequestId.GetValueOrDefault(c.RequestId, string.Empty),
                    RequestProgress = c.Request?.RequestProgress?.Percentage ?? 0,
                    DaysRemaining = (int)Math.Ceiling((c.Deadline - utcNow).TotalDays),
                    ContractStatus = MapContractStatusSlug(c.SLAStatus),
                    Description = c.Request?.Discription ?? string.Empty,
                    SlaUiStatus = MapSlaUiStatus(c.SLAStatus)
                };
            }).ToList();

            var response = new AdminSlaMonitorDTO
            {
                TotalSlaContracts = await slaRepo.CountAsync(new SlaTotalCountSpecification()),
                InProgressCount = await slaRepo.CountAsync(new SlaCountByStatusSpecification(SLAStatus.Inprogress)),
                DelayedCount = await slaRepo.CountAsync(new SlaCountByStatusSpecification(SLAStatus.Delayed)),
                CompletedCount = await slaRepo.CountAsync(new SlaCountByStatusSpecification(SLAStatus.Completed)),
                Contracts = new PaginatedResult<AdminSlaContractMonitorDTO>(queryParams.PageIndex, queryParams.PageSize, count, data)
            };

            return response;
        }

        private static string ResolveWarningLevel(SLAContract contract)
        {
            if (contract.SLAStatus == SLAStatus.Completed)
                return "Completed";

            if (contract.SLAStatus == SLAStatus.Delayed)
                return "Delayed";

            var remainingHours = (contract.Deadline - DateTime.UtcNow).TotalHours;
            if (remainingHours <= 24)
                return "Critical";

            if (remainingHours <= 72)
                return "Warning";

            return "Normal";
        }

        private static void ResolveSlaDisplayFilterFlags(
            string? slaDisplayFilter,
            out bool useSlaDisplayFilter,
            out bool slaNa,
            out bool slaActive,
            out bool slaAtRisk,
            out bool slaDelayed,
            out bool slaCompleted)
        {
            useSlaDisplayFilter = false;
            slaNa = slaActive = slaAtRisk = slaDelayed = slaCompleted = false;

            if (string.IsNullOrWhiteSpace(slaDisplayFilter))
                return;

            var m = slaDisplayFilter.Trim().ToLowerInvariant();
            useSlaDisplayFilter = m switch
            {
                "n/a" or "na" => true,
                "active" => true,
                "at-risk" or "atrisk" => true,
                "delayed" => true,
                "completed" => true,
                _ => false
            };

            if (!useSlaDisplayFilter)
                return;

            slaNa = m is "n/a" or "na";
            slaActive = m == "active";
            slaAtRisk = m is "at-risk" or "atrisk";
            slaDelayed = m == "delayed";
            slaCompleted = m == "completed";
        }

        private static string MapContractStatusSlug(SLAStatus status) => status switch
        {
            SLAStatus.Inprogress => "in-progress",
            SLAStatus.Delayed => "delayed",
            SLAStatus.Completed => "completed",
            _ => "in-progress"
        };

        private static string MapSlaUiStatus(SLAStatus status) => status switch
        {
            SLAStatus.Inprogress => "active",
            SLAStatus.Delayed => "breached",
            SLAStatus.Completed => "completed",
            _ => "active"
        };

        private static string MapWarningLevelUi(string warningLevel) => warningLevel switch
        {
            "Normal" => "none",
            "Warning" => "medium",
            "Critical" => "high",
            "Delayed" => "high",
            "Completed" => "none",
            _ => "none"
        };
    }
}
