using AutoMapper;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.CategoryDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Shared.CommonResult;
using CorpServe.Domain.Contracts;
using CorpServe.Shared;
using CorpServe.Services.Specifications;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<IEnumerable<CategoryLookupDTO>>> GetAllCategoriesAsync()
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var specification = new CategoryOrderByIdSpecification();
            var categories = await categoryRepo.GetAllAsync(specification);
            return _mapper.Map<List<CategoryLookupDTO>>(categories);
        }

        public async Task<CategoryAdminManageDTO> GetAllCategoriesAsync(CategoryQuaryParams quaryParams)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var allMetricsSpecification = new CategoryAdminMetricsSpecification(null);
            var filteredMetricsSpecification = new CategoryAdminMetricsSpecification(quaryParams.Search);

            var allCategoriesForMetrics = (await categoryRepo.GetAllAsync(allMetricsSpecification)).ToList();
            var categoriesForMetrics = (await categoryRepo.GetAllAsync(filteredMetricsSpecification)).ToList();
            var count = categoriesForMetrics.Count;
            var totalCategories = allCategoriesForMetrics.Count;

            var vendorVerifyRepo = _unitOfWork.GetRepository<VendorVerify, string>();
            var approvedVendorIds = (await vendorVerifyRepo.GetAllAsync())
                .Where(v => v.Status == VerifyStatus.Approved)
                .Select(v => v.VendorId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var requestCounts = allCategoriesForMetrics
                .ToDictionary(c => c.Id, c => c.Requests.Count, StringComparer.OrdinalIgnoreCase);
            var maxRequestCount = requestCounts.Count > 0 ? requestCounts.Values.Max() : 0;

            var orderedCategoriesByDemand = allCategoriesForMetrics
                .OrderByDescending(c => c.Requests.Count)
                .ThenBy(c => c.Name)
                .ToList();

            var orderedByDemand = orderedCategoriesByDemand
                .Select((category, index) => new { category.Id, Rank = index + 1 })
                .ToDictionary(x => x.Id, x => x.Rank, StringComparer.OrdinalIgnoreCase);

            var topCategory = orderedCategoriesByDemand.FirstOrDefault();

            var totalVendors = allCategoriesForMetrics
                .SelectMany(c => c.VendorCategories)
                .Select(vc => vc.VendorId)
                .Where(vendorId => approvedVendorIds.Contains(vendorId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var averageRequests = allCategoriesForMetrics.Count > 0
                ? (int)Math.Round(allCategoriesForMetrics.Average(c => c.Requests.Count), MidpointRounding.AwayFromZero)
                : 0;

            var filteredOrderedCategories = categoriesForMetrics
                .OrderBy(c => orderedByDemand.GetValueOrDefault(c.Id, int.MaxValue))
                .ThenBy(c => c.Name)
                .ToList();

            var pagedCategories = filteredOrderedCategories
                .Skip((quaryParams.PageIndex - 1) * quaryParams.PageSize)
                .Take(quaryParams.PageSize)
                .ToList();

            var data = _mapper.Map<List<CategoriesDTO>>(pagedCategories);
            foreach (var item in data)
            {
                var category = pagedCategories.FirstOrDefault(c => string.Equals(c.Id, item.Id, StringComparison.OrdinalIgnoreCase));
                item.VendorCount = category?.VendorCategories
                    .Select(vc => vc.VendorId)
                    .Where(vendorId => approvedVendorIds.Contains(vendorId))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() ?? 0;

                item.RequestCount = requestCounts.GetValueOrDefault(item.Id);
                item.DemandMeter = maxRequestCount == 0
                    ? 0
                    : (int)Math.Round((double)item.RequestCount / maxRequestCount * 100, MidpointRounding.AwayFromZero);
                item.DemandRank = orderedByDemand.GetValueOrDefault(item.Id);
            }

            var paginatedCategories = new PaginatedResult<CategoriesDTO>(quaryParams.PageIndex, quaryParams.PageSize, count, data);

            return new CategoryAdminManageDTO
            {
                Summary = new CategoryAdminSummaryDTO
                {
                    TotalCategories = totalCategories,
                    TotalVendors = totalVendors,
                    AverageRequests = averageRequests,
                    TopCategoryName = topCategory?.Name ?? string.Empty,
                    TopCategoryRequestCount = topCategory?.Requests.Count ?? 0
                },
                Categories = paginatedCategories
            };
        }

        public async Task<Result<CategoriesDTO>> CreateCategoryAsync(string adminId, CreateUpdateCategoryDTO createCategoryDTO)
        {
            if (string.IsNullOrWhiteSpace(adminId))
                return Error.Unauthorized("Category.AdminRequired", "Admin identity is required.");

            var categoryName = createCategoryDTO.CategoryName?.Trim();
            var description = createCategoryDTO.Description?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
                return Error.Validation("Category.NameRequired", "Category name is required.");

            if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
                return Error.Validation("Category.DescriptionTooLong", "Category description cannot exceed 500 characters.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var exists = await categoryRepo.AnyAsync(c => c.Name == categoryName);
            if (exists)
                return Error.Conflict("Category.AlreadyExists", "Category with the same name already exists.");

            var category = new Category
            {
                Name = categoryName,
                Description = description,
                AdminId = adminId
            };

            await categoryRepo.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CategoriesDTO>(category);
        }

        public async Task<Result<bool>> UpdateCategoryAsync(string categoryId, CreateUpdateCategoryDTO updateCategoryDTO)
        {
            var categoryName = updateCategoryDTO.CategoryName?.Trim();
            var description = updateCategoryDTO.Description?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
                return Error.Validation("Category.NameRequired", "Category name is required.");

            if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
                return Error.Validation("Category.DescriptionTooLong", "Category description cannot exceed 500 characters.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var categorySpecification = new CategoryByIdSpecification(categoryId);
            var category = await categoryRepo.GetByIdAsync(categorySpecification);
            if (category is null)
                return Error.NotFound("Category.NotFound", "Category not found.");

            var duplicateNameExists = await categoryRepo.AnyAsync(c => c.Name == categoryName && c.Id != categoryId);
            if (duplicateNameExists)
                return Error.Conflict("Category.AlreadyExists", "Category with the same name already exists.");

            category.Name = categoryName;
            category.Description = description;
            categoryRepo.Update(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<Result<bool>> DeleteCategoryAsync(string categoryId)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var categorySpecification = new CategoryByIdSpecification(categoryId);
            var category = await categoryRepo.GetByIdAsync(categorySpecification);
            if (category is null)
                return Error.NotFound("Category.NotFound", "Category not found.");

            if (category.VendorCategories.Any())
                return Error.Conflict("Category.HasVendors", "Cannot delete category with assigned vendors.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var hasRequests = await requestRepo.AnyAsync(r => r.CateogryId == categoryId);
            if (hasRequests)
                return Error.Conflict("Category.HasRequests", "Cannot delete category with assigned Requests.");

            categoryRepo.Remove(category);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Error.Conflict("Category.DeleteFailed", ex.InnerException?.Message ?? ex.Message);
            }

            return true;
        }
    }
}
