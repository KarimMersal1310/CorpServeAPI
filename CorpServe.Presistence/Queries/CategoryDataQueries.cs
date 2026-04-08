using AutoMapper;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Presistence.Data.DbContext;
using CorpServe.Services.Abstraction;
using CorpServe.Shared;
using CorpServe.Shared.DTOs.CategoryDTOs;
using CorpServe.Shared.QueryParams;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Presistence.Queries
{
    public sealed class CategoryDataQueries : ICategoryDataQueries
    {
        private readonly CorpServeDbContext _dbContext;
        private readonly IMapper _mapper;

        public CategoryDataQueries(CorpServeDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<string>> GetInvalidCategoryIdsAsync(IReadOnlyList<string> candidateIds, CancellationToken cancellationToken = default)
        {
            if (candidateIds.Count == 0)
                return Array.Empty<string>();

            var distinct = candidateIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var found = await _dbContext.Categories.AsNoTracking()
                .Where(c => distinct.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var foundSet = found.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return distinct.Where(id => !foundSet.Contains(id)).ToList();
        }

        public async Task<CategoryAdminManageDTO> GetAdminManageAsync(CategoryQuaryParams queryParams, CancellationToken cancellationToken = default)
        {
            var search = queryParams.Search?.Trim();

            var allCategories = await _dbContext.Categories.AsNoTracking().ToListAsync(cancellationToken);

            var requestCountsList = await _dbContext.Set<Request>().AsNoTracking()
                .GroupBy(r => r.CateogryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var requestCounts = requestCountsList.ToDictionary(x => x.CategoryId, x => x.Count, StringComparer.OrdinalIgnoreCase);

            var maxRequestCount = requestCounts.Count > 0 ? requestCounts.Values.Max() : 0;
            var totalCategories = allCategories.Count;

            var totalVendors = await _dbContext.Set<VendorCategory>().AsNoTracking()
                .Where(vc => _dbContext.Set<VendorVerify>().Any(v => v.VendorId == vc.VendorId && v.Status == VerifyStatus.Approved))
                .Select(vc => vc.VendorId)
                .Distinct()
                .CountAsync(cancellationToken);

            var averageRequests = totalCategories > 0
                ? (int)Math.Round(allCategories.Average(c => requestCounts.GetValueOrDefault(c.Id, 0)), MidpointRounding.AwayFromZero)
                : 0;

            var orderedCategoriesByDemand = allCategories
                .OrderByDescending(c => requestCounts.GetValueOrDefault(c.Id, 0))
                .ThenBy(c => c.Name)
                .ToList();

            var topCategory = orderedCategoriesByDemand.FirstOrDefault();

            var orderedByDemand = orderedCategoriesByDemand
                .Select((category, index) => new { category.Id, Rank = index + 1 })
                .ToDictionary(x => x.Id, x => x.Rank, StringComparer.OrdinalIgnoreCase);

            var filtered = allCategories
                .Where(c => string.IsNullOrWhiteSpace(search)
                    || c.Name.Contains(search)
                    || (c.Description != null && c.Description.Contains(search)))
                .ToList();

            var count = filtered.Count;

            var filteredOrdered = filtered
                .OrderBy(c => orderedByDemand.GetValueOrDefault(c.Id, int.MaxValue))
                .ThenBy(c => c.Name)
                .ToList();

            var pagedCategories = filteredOrdered
                .Skip((queryParams.PageIndex - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            var pagedIds = pagedCategories.Select(c => c.Id).ToList();

            Dictionary<string, int> vendorCountsPage;
            if (pagedIds.Count == 0)
            {
                vendorCountsPage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                var vendorCountRows = await _dbContext.Set<VendorCategory>().AsNoTracking()
                    .Where(vc => pagedIds.Contains(vc.CategoryId) && _dbContext.Set<VendorVerify>().Any(v => v.VendorId == vc.VendorId && v.Status == VerifyStatus.Approved))
                    .GroupBy(vc => vc.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Select(v => v.VendorId).Distinct().Count() })
                    .ToListAsync(cancellationToken);

                vendorCountsPage = vendorCountRows.ToDictionary(x => x.CategoryId, x => x.Count, StringComparer.OrdinalIgnoreCase);
            }

            var data = _mapper.Map<List<CategoriesDTO>>(pagedCategories);
            foreach (var item in data)
            {
                var rc = requestCounts.GetValueOrDefault(item.Id, 0);
                item.VendorCount = vendorCountsPage.GetValueOrDefault(item.Id, 0);
                item.RequestCount = rc;
                item.DemandMeter = maxRequestCount == 0
                    ? 0
                    : (int)Math.Round((double)rc / maxRequestCount * 100, MidpointRounding.AwayFromZero);
                item.DemandRank = orderedByDemand.GetValueOrDefault(item.Id);
            }

            var paginatedCategories = new PaginatedResult<CategoriesDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);

            return new CategoryAdminManageDTO
            {
                Summary = new CategoryAdminSummaryDTO
                {
                    TotalCategories = totalCategories,
                    TotalVendors = totalVendors,
                    AverageRequests = averageRequests,
                    TopCategoryName = topCategory?.Name ?? string.Empty,
                    TopCategoryRequestCount = topCategory is null ? 0 : requestCounts.GetValueOrDefault(topCategory.Id, 0)
                },
                Categories = paginatedCategories
            };
        }
    }
}
