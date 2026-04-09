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

        public async Task<PaginatedResult<AdminUserManagementDTO>> GetUsersForManagementAsync(AdminUserManagementQueryParams queryParams)
        {
            var normalizedRole = queryParams.Role?.Trim().ToLowerInvariant();
            var includeClient = string.IsNullOrWhiteSpace(normalizedRole) || normalizedRole == "client";
            var includeVendor = string.IsNullOrWhiteSpace(normalizedRole) || normalizedRole == "vendor";

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

            IEnumerable<ApplicationUser> filteredUsers = users.Values;

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
                    Status = user.Status.ToString(),
                    RequestsCreatedCount = requestsCreatedCount,
                    RequestsHandledCount = requestsHandledCount
                });
            }

            return new PaginatedResult<AdminUserManagementDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
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

        public async Task<PaginatedResult<AdminRequestMonitorDTO>> GetRequestMonitorAsync(AdminRequestMonitorQueryParams queryParams)
        {
            var requestRepo = _unitOfWork.GetRepository<Request, string>();

            var listSpecification = new AdminRequestMonitorListSpecification(
                queryParams.Search,
                queryParams.CategoryId,
                queryParams.RequestStatus,
                queryParams.SlaStatus,
                queryParams.PageSize,
                queryParams.PageIndex);

            var countSpecification = new AdminRequestMonitorCountSpecification(
                queryParams.Search,
                queryParams.CategoryId,
                queryParams.RequestStatus,
                queryParams.SlaStatus);

            var requests = (await requestRepo.GetAllAsync(listSpecification)).ToList();
            var count = await requestRepo.CountAsync(countSpecification);

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
                    Price = r.SLAContract?.ContractPrice ?? selectedProposal?.ProposedPrice,
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

            return new PaginatedResult<AdminRequestMonitorDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }

        public async Task<Result<AdminSlaMonitorDTO>> GetSlaMonitorAsync(AdminSlaMonitorQueryParams queryParams)
        {
            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();

            var listSpecification = new AdminSlaMonitorListSpecification(
                queryParams.Search,
                queryParams.SlaStatus,
                queryParams.PageSize,
                queryParams.PageIndex);

            var countSpecification = new AdminSlaMonitorCountSpecification(queryParams.Search, queryParams.SlaStatus);

            var contracts = (await slaRepo.GetAllAsync(listSpecification)).ToList();
            var count = await slaRepo.CountAsync(countSpecification);

            var data = contracts.Select(c => new AdminSlaContractMonitorDTO
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
                WarningLevel = ResolveWarningLevel(c)
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
    }
}
